using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextBus.API.Controllers;
using NextBus.Presentation.Wallets.Command;
using NextBus.Presentation.Wallets.Models.Result;
using NextBus.Presentation.Wallets.Queries;

namespace NextBus.Api.Controllers
{
    //[Route("[controller]"), ApiController, Authorize]
     public class WalletsController : BaseController
    {
          
          private readonly IMapper _mapper;

          public WalletsController(IMapper mapper)
          {
              
               _mapper = mapper;
          }

          [HttpPost("AddFunds", Name = "AddFunds"), ProducesResponseType(typeof(GetWalletQueryResult), StatusCodes.Status201Created), ProducesDefaultResponseType]
          public async Task<IActionResult> AddFunds([FromBody] AddFundToWalletCommand command)
          {
               var result = await Mediator.Send(command);
               var history = _mapper.Map<GetWalletHistoryQueryResult>(result);
               return CreatedAtRoute("GetWalletHistory", new { historyid = result.Id }, history);
          }

          [HttpPost("RemoveFunds", Name = "RemoveFunds"), ProducesResponseType(typeof(GetWalletQueryResult), StatusCodes.Status201Created), ProducesDefaultResponseType]
          public async Task<IActionResult> RemoveFunds([FromBody] RemoveFundFromWalletCommand command)
          {
               var result = await Mediator.Send(command);
               var history = _mapper.Map<GetWalletHistoryQueryResult>(result);
               return CreatedAtRoute("GetWalletHistory", new { historyid = result.Id }, history);
          }

          [HttpGet("histories", Name = "GetWalletHistories"), ProducesResponseType(typeof(IEnumerable<GetWalletHistoryQueryResult>), StatusCodes.Status201Created), ProducesDefaultResponseType]
          public async Task<IActionResult> GetWalletHistories()
          {
               var result = await Mediator.Send(new GetWalletHistoriesQuery());
               return Ok(result);
          }

        [HttpGet("{userid}/histories", Name = "GetWalletHistoriesForUser"), ProducesResponseType(typeof(IEnumerable<GetWalletHistoryQueryResult>), StatusCodes.Status201Created), ProducesDefaultResponseType]
        public async Task<IActionResult> GetWalletHistoriesForUser(string userid)
        {
            var result = await Mediator.Send(new GetWalletHistoriesQuery { AppUserId = userid });
            return Ok(result);
        }

        [HttpGet(Name = "GetWallets"), ProducesResponseType(typeof(IEnumerable<GetWalletQueryResult>), StatusCodes.Status200OK), ProducesDefaultResponseType]
          public async Task<IActionResult> GetWallets()
          {
               var wallets = await Mediator.Send(new GetWalletsQuery());
               return Ok(wallets);
          }

          [HttpGet("{userid}", Name = "GetWallet"), ProducesResponseType(typeof(GetWalletQueryResult), StatusCodes.Status200OK), ProducesDefaultResponseType]
          public async Task<IActionResult> GetWallet(string userid)
          {
               var wallet = await Mediator.Send(new GetWalletQuery { AppUserId = userid });
               return Ok(wallet);
          }


          [HttpGet("{historyid}/history", Name = "GetWalletHistory"), ProducesResponseType(typeof(GetWalletHistoryQueryResult), StatusCodes.Status200OK), ProducesDefaultResponseType]
          public async Task<IActionResult> GetWalletHistory(Guid historyid)
          {
               var history = await Mediator.Send(new GetWalletHistoryQuery() { Id = historyid });
               return Ok(history);
          }
     }
}