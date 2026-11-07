using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Common.Extensions;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;

namespace NextBus.Application.Features.Wallets
{
    // 1. Top Up Wallet
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
                Details = $"₦{request.Dto.Amount:N2} topped up into wallet via payment gateway",
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

    // 2. Withdraw from Wallet
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

            return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet), "Withdrawal initiated successfully.");
        }
    }

    // 3. Get My Wallet
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
}
