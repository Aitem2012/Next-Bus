using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Application.Features.Wallets;

namespace NextBus.API.Controllers
{
    [Authorize]
    public class WalletsController : BaseApiController
    {
        [HttpGet("my-wallet")]
        [ProducesResponseType(typeof(Result<WalletDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyWallet()
        {
            var result = await Mediator.Send(new GetMyWalletQuery());
            return HandleResult(result);
        }

        /// <summary>
        /// Initialize Paystack payment to fund wallet
        /// </summary>
        [HttpPost("paystack/initialize")]
        [ProducesResponseType(typeof(Result<PaystackInitResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<PaystackInitResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> InitializePaystackTopUp([FromBody] InitializeFundWalletDto dto)
        {
            var result = await Mediator.Send(new InitializePaystackTopUpCommand(dto));
            return HandleResult(result);
        }

        /// <summary>
        /// Verify Paystack payment and credit user wallet
        /// </summary>
        [HttpPost("paystack/verify")]
        [ProducesResponseType(typeof(Result<WalletDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<WalletDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyPaystackTopUp([FromBody] VerifyFundWalletDto dto)
        {
            var result = await Mediator.Send(new VerifyPaystackTopUpCommand(dto));
            return HandleResult(result);
        }

        /// <summary>
        /// Step 1 of Withdrawal: Verify destination bank account and send OTP to user's email
        /// </summary>
        [HttpPost("withdraw/request-otp")]
        [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestWithdrawalOtp([FromBody] RequestWithdrawalDto dto)
        {
            var result = await Mediator.Send(new RequestWithdrawalOtpCommand(dto));
            return HandleResult(result);
        }

        /// <summary>
        /// Step 2 of Withdrawal: Provide the 6-digit OTP sent via email to execute Paystack transfer
        /// </summary>
        [HttpPost("withdraw/confirm")]
        [ProducesResponseType(typeof(Result<WalletDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<WalletDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmWithdrawal([FromBody] ConfirmWithdrawalDto dto)
        {
            var result = await Mediator.Send(new ConfirmWithdrawalCommand(dto));
            return HandleResult(result);
        }

        /// <summary>
        /// Direct wallet top-up (simulation/fallback)
        /// </summary>
        [HttpPost("top-up")]
        [ProducesResponseType(typeof(Result<WalletDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<WalletDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TopUp([FromBody] TopUpWalletDto dto)
        {
            var result = await Mediator.Send(new TopUpWalletCommand(dto));
            return HandleResult(result);
        }

        /// <summary>
        /// Direct withdrawal (simulation/fallback)
        /// </summary>
        [HttpPost("withdraw")]
        [ProducesResponseType(typeof(Result<WalletDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<WalletDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawFundDto dto)
        {
            var result = await Mediator.Send(new WithdrawFundCommand(dto));
            return HandleResult(result);
        }
    }
}
