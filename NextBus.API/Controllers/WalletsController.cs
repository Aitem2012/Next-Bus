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

        [HttpPost("top-up")]
        [ProducesResponseType(typeof(Result<WalletDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<WalletDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TopUp([FromBody] TopUpWalletDto dto)
        {
            var result = await Mediator.Send(new TopUpWalletCommand(dto));
            return HandleResult(result);
        }

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
