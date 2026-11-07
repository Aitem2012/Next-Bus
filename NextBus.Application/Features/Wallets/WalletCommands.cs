using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Common.Extensions;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace NextBus.Application.Features.Wallets
{
    // ==========================================
    // 1. Paystack Wallet Funding - Initialize
    // ==========================================
    public record InitializePaystackTopUpCommand(InitializeFundWalletDto Dto) : IRequest<Result<PaystackInitResponseDto>>;

    public class InitializePaystackTopUpCommandValidator : AbstractValidator<InitializePaystackTopUpCommand>
    {
        public InitializePaystackTopUpCommandValidator()
        {
            RuleFor(x => x.Dto.Amount).GreaterThan(0).WithMessage("Top-up amount must be greater than 0.");
        }
    }

    public class InitializePaystackTopUpCommandHandler : IRequestHandler<InitializePaystackTopUpCommand, Result<PaystackInitResponseDto>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IPaystackService _paystackService;

        public InitializePaystackTopUpCommandHandler(
            ICurrentUserService currentUserService,
            UserManager<AppUser> userManager,
            IPaystackService paystackService)
        {
            _currentUserService = currentUserService;
            _userManager = userManager;
            _paystackService = paystackService;
        }

        public async Task<Result<PaystackInitResponseDto>> Handle(InitializePaystackTopUpCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<PaystackInitResponseDto>.Failure("Unauthorized.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<PaystackInitResponseDto>.Failure("User not found.");
            }

            var reference = "NXBS_TOP_".GenerateRef();
            var callbackUrl = !string.IsNullOrWhiteSpace(request.Dto.CallbackUrl)
                ? request.Dto.CallbackUrl
                : "https://dev-nextbus.u-check.ng/wallet";

            return await _paystackService.InitializePaymentAsync(user.Email!, request.Dto.Amount, reference, callbackUrl);
        }
    }

    // ==========================================
    // 2. Paystack Wallet Funding - Verify & Credit
    // ==========================================
    public record VerifyPaystackTopUpCommand(VerifyFundWalletDto Dto) : IRequest<Result<WalletDto>>;

    public class VerifyPaystackTopUpCommandValidator : AbstractValidator<VerifyPaystackTopUpCommand>
    {
        public VerifyPaystackTopUpCommandValidator()
        {
            RuleFor(x => x.Dto.Reference).NotEmpty().WithMessage("Payment reference is required.");
        }
    }

    public class VerifyPaystackTopUpCommandHandler : IRequestHandler<VerifyPaystackTopUpCommand, Result<WalletDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPaystackService _paystackService;
        private readonly IMapper _mapper;

        public VerifyPaystackTopUpCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            IPaystackService paystackService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _paystackService = paystackService;
            _mapper = mapper;
        }

        public async Task<Result<WalletDto>> Handle(VerifyPaystackTopUpCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<WalletDto>.Failure("Unauthorized.");
            }

            // Verify with Paystack
            var verifyResult = await _paystackService.VerifyPaymentAsync(request.Dto.Reference);
            if (!verifyResult.Succeeded || verifyResult.Data == null || !verifyResult.Data.IsSuccessful)
            {
                return Result<WalletDto>.Failure(verifyResult.Errors.Length > 0 ? verifyResult.Errors : new[] { "Payment verification was unsuccessful." });
            }

            // Prevent duplicate crediting
            var alreadyProcessed = await _context.Transactions
                .AnyAsync(t => t.Reference == request.Dto.Reference && t.IsSuccessful, cancellationToken);

            var wallet = await _context.Wallets
                .Include(w => w.Histories)
                .SingleOrDefaultAsync(w => w.AppUserId == userId, cancellationToken);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    AppUserId = userId,
                    Balance = 0m,
                    Reference = "NXBS_W".GenerateRef(),
                    LastUpdated = DateTime.UtcNow
                };
                await _context.Wallets.AddAsync(wallet, cancellationToken);
            }

            if (alreadyProcessed)
            {
                return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet), "Payment was already verified and credited.");
            }

            var creditedAmount = verifyResult.Data.Amount;
            wallet.Balance += creditedAmount;
            wallet.LastUpdated = DateTime.UtcNow;

            var history = new WalletHistory
            {
                WalletId = wallet.Id,
                AppUserId = userId,
                Amount = creditedAmount,
                Type = "TopUp",
                Reference = request.Dto.Reference,
                Details = $"₦{creditedAmount:N2} credited via Paystack ({verifyResult.Data.Channel})",
                Date = DateTime.UtcNow
            };

            var transaction = new Transaction
            {
                AppUserId = userId,
                Amount = creditedAmount,
                Type = TransactionType.TopUp,
                Reference = request.Dto.Reference,
                Description = history.Details,
                IsSuccessful = true,
                Date = DateTime.UtcNow
            };

            await _context.WalletHistories.AddAsync(history, cancellationToken);
            await _context.Transactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet), $"Wallet credited with ₦{creditedAmount:N2} successfully.");
        }
    }

    // ==========================================
    // 3. Paystack Withdrawal - Step 1: Request OTP
    // ==========================================
    public record RequestWithdrawalOtpCommand(RequestWithdrawalDto Dto) : IRequest<Result<string>>;

    public class RequestWithdrawalOtpCommandValidator : AbstractValidator<RequestWithdrawalOtpCommand>
    {
        public RequestWithdrawalOtpCommandValidator()
        {
            RuleFor(x => x.Dto.Amount).GreaterThan(0).WithMessage("Withdrawal amount must be greater than 0.");
            RuleFor(x => x.Dto.AccountNumber).NotEmpty().Length(10).WithMessage("Account number must be 10 digits.");
            RuleFor(x => x.Dto.BankCode).NotEmpty().WithMessage("Bank code is required.");
        }
    }

    public class RequestWithdrawalOtpCommandHandler : IRequestHandler<RequestWithdrawalOtpCommand, Result<string>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IPaystackService _paystackService;
        private readonly IOtpService _otpService;

        public RequestWithdrawalOtpCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            UserManager<AppUser> userManager,
            IPaystackService paystackService,
            IOtpService otpService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _userManager = userManager;
            _paystackService = paystackService;
            _otpService = otpService;
        }

        public async Task<Result<string>> Handle(RequestWithdrawalOtpCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<string>.Failure("Unauthorized.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<string>.Failure("User not found.");
            }

            var wallet = await _context.Wallets.SingleOrDefaultAsync(w => w.AppUserId == userId, cancellationToken);
            if (wallet == null || wallet.Balance < request.Dto.Amount)
            {
                var balance = wallet?.Balance ?? 0m;
                return Result<string>.Failure($"Insufficient wallet balance. Current balance: ₦{balance:N2}. Requested: ₦{request.Dto.Amount:N2}.");
            }

            // Verify/Resolve Destination Bank Account with Paystack
            var resolveResult = await _paystackService.ResolveAccountNumberAsync(request.Dto.AccountNumber, request.Dto.BankCode);
            if (!resolveResult.Succeeded)
            {
                return Result<string>.Failure($"Destination account verification failed: {string.Join(", ", resolveResult.Errors)}");
            }

            var resolvedAccountName = string.IsNullOrWhiteSpace(resolveResult.Data?.AccountName)
                ? request.Dto.AccountName
                : resolveResult.Data.AccountName;

            var metadataObj = new
            {
                amount = request.Dto.Amount,
                accountNumber = request.Dto.AccountNumber,
                bankCode = request.Dto.BankCode,
                accountName = resolvedAccountName
            };
            var metadataJson = JsonSerializer.Serialize(metadataObj);

            // Generate and send withdrawal OTP
            await _otpService.GenerateAndSendOtpAsync(user.Email!, user.Id, OtpPurpose.Withdrawal, metadataJson);

            return Result<string>.Success(
                resolvedAccountName,
                $"Withdrawal authorization OTP has been sent to {user.Email}. Destination Account: {resolvedAccountName} ({request.Dto.AccountNumber}).");
        }
    }

    // ==========================================
    // 4. Paystack Withdrawal - Step 2: Confirm OTP
    // ==========================================
    public record ConfirmWithdrawalCommand(ConfirmWithdrawalDto Dto) : IRequest<Result<WalletDto>>;

    public class ConfirmWithdrawalCommandValidator : AbstractValidator<ConfirmWithdrawalCommand>
    {
        public ConfirmWithdrawalCommandValidator()
        {
            RuleFor(x => x.Dto.OtpCode).NotEmpty().Length(6).WithMessage("OTP code must be 6 digits.");
        }
    }

    public class ConfirmWithdrawalCommandHandler : IRequestHandler<ConfirmWithdrawalCommand, Result<WalletDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IPaystackService _paystackService;
        private readonly IOtpService _otpService;
        private readonly IMapper _mapper;

        public ConfirmWithdrawalCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            UserManager<AppUser> userManager,
            IPaystackService paystackService,
            IOtpService otpService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _userManager = userManager;
            _paystackService = paystackService;
            _otpService = otpService;
            _mapper = mapper;
        }

        public async Task<Result<WalletDto>> Handle(ConfirmWithdrawalCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<WalletDto>.Failure("Unauthorized.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<WalletDto>.Failure("User not found.");
            }

            // Verify OTP
            var verifyResult = await _otpService.VerifyOtpAsync(user.Email!, request.Dto.OtpCode, OtpPurpose.Withdrawal);
            if (!verifyResult.Succeeded)
            {
                return Result<WalletDto>.Failure(verifyResult.Errors);
            }

            var otpEntity = verifyResult.Data;
            if (otpEntity == null || string.IsNullOrEmpty(otpEntity.Metadata))
            {
                return Result<WalletDto>.Failure("Withdrawal metadata is missing or corrupted. Please request a new withdrawal.");
            }

            decimal amount;
            string accountNumber;
            string bankCode;
            string accountName;

            try
            {
                using var doc = JsonDocument.Parse(otpEntity.Metadata);
                var root = doc.RootElement;
                amount = root.GetProperty("amount").GetDecimal();
                accountNumber = root.GetProperty("accountNumber").GetString() ?? "";
                bankCode = root.GetProperty("bankCode").GetString() ?? "";
                accountName = root.GetProperty("accountName").GetString() ?? "";
            }
            catch (Exception)
            {
                return Result<WalletDto>.Failure("Invalid withdrawal request data.");
            }

            var wallet = await _context.Wallets
                .Include(w => w.Histories)
                .SingleOrDefaultAsync(w => w.AppUserId == userId, cancellationToken);

            if (wallet == null || wallet.Balance < amount)
            {
                return Result<WalletDto>.Failure("Insufficient wallet balance for this withdrawal.");
            }

            // Create Paystack transfer recipient
            var recipientResult = await _paystackService.CreateTransferRecipientAsync(accountName, accountNumber, bankCode);
            if (!recipientResult.Succeeded)
            {
                return Result<WalletDto>.Failure($"Failed to create transfer recipient: {string.Join(", ", recipientResult.Errors)}");
            }

            var reference = "WDRW_".GenerateRef();

            // Initiate Paystack transfer
            var transferResult = await _paystackService.InitiateTransferAsync(
                amount,
                recipientResult.Data!.RecipientCode,
                "NextBus Wallet Withdrawal",
                reference);

            if (!transferResult.Succeeded)
            {
                return Result<WalletDto>.Failure($"Transfer initiation failed: {string.Join(", ", transferResult.Errors)}");
            }

            // Debit wallet balance
            wallet.Balance -= amount;
            wallet.LastUpdated = DateTime.UtcNow;

            var history = new WalletHistory
            {
                WalletId = wallet.Id,
                AppUserId = userId,
                Amount = amount,
                Type = "Withdrawal",
                Reference = reference,
                Details = $"₦{amount:N2} withdrawn to {accountName} ({accountNumber}) via Paystack",
                Date = DateTime.UtcNow
            };

            var transaction = new Transaction
            {
                AppUserId = userId,
                Amount = amount,
                Type = TransactionType.Withdrawal,
                Reference = reference,
                Description = history.Details,
                IsSuccessful = true,
                Date = DateTime.UtcNow
            };

            await _context.WalletHistories.AddAsync(history, cancellationToken);
            await _context.Transactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<WalletDto>.Success(
                _mapper.Map<WalletDto>(wallet),
                $"Withdrawal of ₦{amount:N2} to {accountName} ({accountNumber}) completed successfully.");
        }
    }

    // ==========================================
    // 5. Direct Top Up Wallet (Simulation / Fallback)
    // ==========================================
    public record TopUpWalletCommand(TopUpWalletDto Dto) : IRequest<Result<WalletDto>>;

    public class TopUpWalletCommandValidator : AbstractValidator<TopUpWalletCommand>
    {
        public TopUpWalletCommandValidator()
        {
            RuleFor(x => x.Dto.Amount).GreaterThan(0).WithMessage("Top-up amount must be greater than 0.");
        }
    }

    public class TopUpWalletCommandHandler : IRequestHandler<TopUpWalletCommand, Result<WalletDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public TopUpWalletCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<WalletDto>> Handle(TopUpWalletCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<WalletDto>.Failure("Unauthorized.");
            }

            var wallet = await _context.Wallets
                .Include(w => w.Histories)
                .SingleOrDefaultAsync(w => w.AppUserId == userId, cancellationToken);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    AppUserId = userId,
                    Balance = 0m,
                    Reference = "NXBS_W".GenerateRef(),
                    LastUpdated = DateTime.UtcNow
                };
                await _context.Wallets.AddAsync(wallet, cancellationToken);
            }

            wallet.Balance += request.Dto.Amount;
            wallet.LastUpdated = DateTime.UtcNow;

            var history = new WalletHistory
            {
                WalletId = wallet.Id,
                AppUserId = userId,
                Amount = request.Dto.Amount,
                Type = "TopUp",
                Reference = request.Dto.PaymentReference ?? "PAY".GenerateRef(),
                Details = $"₦{request.Dto.Amount:N2} topped up into wallet",
                Date = DateTime.UtcNow
            };

            var transaction = new Transaction
            {
                AppUserId = userId,
                Amount = request.Dto.Amount,
                Type = TransactionType.TopUp,
                Reference = history.Reference,
                Description = history.Details,
                IsSuccessful = true,
                Date = DateTime.UtcNow
            };

            await _context.WalletHistories.AddAsync(history, cancellationToken);
            await _context.Transactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet), "Wallet topped up successfully.");
        }
    }

    // ==========================================
    // 6. Direct Withdraw (Simulation / Fallback)
    // ==========================================
    public record WithdrawFundCommand(WithdrawFundDto Dto) : IRequest<Result<WalletDto>>;

    public class WithdrawFundCommandValidator : AbstractValidator<WithdrawFundCommand>
    {
        public WithdrawFundCommandValidator()
        {
            RuleFor(x => x.Dto.Amount).GreaterThan(0).WithMessage("Withdrawal amount must be greater than 0.");
            RuleFor(x => x.Dto.AccountNumber).NotEmpty().Length(10).WithMessage("Account number must be 10 digits.");
            RuleFor(x => x.Dto.BankCode).NotEmpty().WithMessage("Bank code is required.");
        }
    }

    public class WithdrawFundCommandHandler : IRequestHandler<WithdrawFundCommand, Result<WalletDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public WithdrawFundCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<WalletDto>> Handle(WithdrawFundCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<WalletDto>.Failure("Unauthorized.");
            }

            var wallet = await _context.Wallets
                .Include(w => w.Histories)
                .SingleOrDefaultAsync(w => w.AppUserId == userId, cancellationToken);

            if (wallet == null)
            {
                return Result<WalletDto>.Failure("Wallet not found.");
            }

            if (wallet.Balance < request.Dto.Amount)
            {
                return Result<WalletDto>.Failure($"Insufficient funds. Current balance is ₦{wallet.Balance:N2}.");
            }

            wallet.Balance -= request.Dto.Amount;
            wallet.LastUpdated = DateTime.UtcNow;

            var history = new WalletHistory
            {
                WalletId = wallet.Id,
                AppUserId = userId,
                Amount = request.Dto.Amount,
                Type = "Withdrawal",
                Reference = "WDRW".GenerateRef(),
                Details = $"₦{request.Dto.Amount:N2} withdrawn to {request.Dto.AccountNumber}",
                Date = DateTime.UtcNow
            };

            var transaction = new Transaction
            {
                AppUserId = userId,
                Amount = request.Dto.Amount,
                Type = TransactionType.Withdrawal,
                Reference = history.Reference,
                Description = history.Details,
                IsSuccessful = true,
                Date = DateTime.UtcNow
            };

            await _context.WalletHistories.AddAsync(history, cancellationToken);
            await _context.Transactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet), "Withdrawal completed successfully.");
        }
    }

    // ==========================================
    // 7. Get My Wallet
    // ==========================================
    public record GetMyWalletQuery : IRequest<Result<WalletDto>>;

    public class GetMyWalletQueryHandler : IRequestHandler<GetMyWalletQuery, Result<WalletDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetMyWalletQueryHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<WalletDto>> Handle(GetMyWalletQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<WalletDto>.Failure("Unauthorized.");
            }

            var wallet = await _context.Wallets
                .Include(w => w.Histories.OrderByDescending(h => h.Date).Take(20))
                .SingleOrDefaultAsync(w => w.AppUserId == userId, cancellationToken);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    AppUserId = userId,
                    Balance = 0m,
                    Reference = "NXBS_W".GenerateRef(),
                    LastUpdated = DateTime.UtcNow
                };
                await _context.Wallets.AddAsync(wallet, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet));
        }
    }

    // ==========================================
    // 8. Paystack Webhook Handler
    // ==========================================
    public record ProcessPaystackWebhookCommand(string Payload, string? SignatureHeader) : IRequest<Result>;

    public class ProcessPaystackWebhookCommandHandler : IRequestHandler<ProcessPaystackWebhookCommand, Result>
    {
        private readonly IAppDbContext _context;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly Microsoft.Extensions.Logging.ILogger<ProcessPaystackWebhookCommandHandler> _logger;

        public ProcessPaystackWebhookCommandHandler(
            IAppDbContext context,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            Microsoft.Extensions.Logging.ILogger<ProcessPaystackWebhookCommandHandler> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Result> Handle(ProcessPaystackWebhookCommand request, CancellationToken cancellationToken)
        {
            var secretKey = _configuration["Paystack:SecretKey"];

            // Verify signature if secret key and header are provided
            if (!string.IsNullOrEmpty(secretKey) && !string.IsNullOrEmpty(request.SignatureHeader))
            {
                using var hmac = new System.Security.Cryptography.HMACSHA512(System.Text.Encoding.UTF8.GetBytes(secretKey));
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Payload));
                var computedSignature = Convert.ToHexString(hash).ToLowerInvariant();

                if (!string.Equals(computedSignature, request.SignatureHeader.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid Paystack webhook signature received.");
                    return Result.Failure("Invalid signature.");
                }
            }

            try
            {
                using var doc = JsonDocument.Parse(request.Payload);
                var root = doc.RootElement;
                var eventName = root.TryGetProperty("event", out var ev) ? ev.GetString() : null;

                if (eventName == "charge.success")
                {
                    var data = root.GetProperty("data");
                    var reference = data.GetProperty("reference").GetString();
                    var amountInKobo = data.GetProperty("amount").GetDecimal();
                    var amount = amountInKobo / 100m;
                    var email = data.GetProperty("customer").GetProperty("email").GetString();

                    if (string.IsNullOrEmpty(reference) || string.IsNullOrEmpty(email))
                    {
                        return Result.Failure("Invalid payload data.");
                    }

                    // Check if already processed
                    var alreadyProcessed = await _context.Transactions
                        .AnyAsync(t => t.Reference == reference && t.IsSuccessful, cancellationToken);

                    if (alreadyProcessed)
                    {
                        return Result.Success("Charge event already processed.");
                    }

                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
                    if (user == null)
                    {
                        _logger.LogWarning("User with email {Email} not found for webhook charge {Ref}", email, reference);
                        return Result.Failure("User not found.");
                    }

                    var wallet = await _context.Wallets.SingleOrDefaultAsync(w => w.AppUserId == user.Id, cancellationToken);
                    if (wallet == null)
                    {
                        wallet = new Wallet
                        {
                            AppUserId = user.Id,
                            Balance = 0m,
                            Reference = "NXBS_W".GenerateRef(),
                            LastUpdated = DateTime.UtcNow
                        };
                        await _context.Wallets.AddAsync(wallet, cancellationToken);
                    }

                    wallet.Balance += amount;
                    wallet.LastUpdated = DateTime.UtcNow;

                    var history = new WalletHistory
                    {
                        WalletId = wallet.Id,
                        AppUserId = user.Id,
                        Amount = amount,
                        Type = "TopUp",
                        Reference = reference,
                        Details = $"₦{amount:N2} credited via Paystack Webhook",
                        Date = DateTime.UtcNow
                    };

                    var transaction = new Transaction
                    {
                        AppUserId = user.Id,
                        Amount = amount,
                        Type = TransactionType.TopUp,
                        Reference = reference,
                        Description = history.Details,
                        IsSuccessful = true,
                        Date = DateTime.UtcNow
                    };

                    await _context.WalletHistories.AddAsync(history, cancellationToken);
                    await _context.Transactions.AddAsync(transaction, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Successfully credited user {UserId} with ₦{Amount} via Paystack webhook", user.Id, amount);
                    return Result.Success("Wallet credited successfully.");
                }
                else if (eventName == "transfer.failed" || eventName == "transfer.reversed")
                {
                    // Refund failed withdrawal
                    var data = root.GetProperty("data");
                    var reference = data.GetProperty("reference").GetString();
                    var amountInKobo = data.GetProperty("amount").GetDecimal();
                    var amount = amountInKobo / 100m;

                    var originalTx = await _context.Transactions
                        .FirstOrDefaultAsync(t => t.Reference == reference && t.Type == TransactionType.Withdrawal, cancellationToken);

                    if (originalTx != null)
                    {
                        var wallet = await _context.Wallets.SingleOrDefaultAsync(w => w.AppUserId == originalTx.AppUserId, cancellationToken);
                        if (wallet != null)
                        {
                            wallet.Balance += amount;
                            wallet.LastUpdated = DateTime.UtcNow;

                            var refundHistory = new WalletHistory
                            {
                                WalletId = wallet.Id,
                                AppUserId = originalTx.AppUserId,
                                Amount = amount,
                                Type = "Refund",
                                Reference = "RFND_".GenerateRef(),
                                Details = $"Refund for failed Paystack withdrawal (Ref: {reference})",
                                Date = DateTime.UtcNow
                            };

                            await _context.WalletHistories.AddAsync(refundHistory, cancellationToken);
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                    }
                }

                return Result.Success("Webhook processed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Paystack webhook.");
                return Result.Failure($"Error processing webhook: {ex.Message}");
            }
        }
    }
}
