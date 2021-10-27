using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextBus.API.Controllers;
using NextBus.Presentation.Common.Models.Results;
using NextBus.Presentation.Transactions.Commands;
using NextBus.Presentation.Transactions.Models.Result;
using NextBus.Presentation.Transactions.Queries;

namespace NextBus.Api.Controllers
{
    /// <summary>
    /// This controller handles all request for transactions
    /// </summary>

    //[Route("[controller]"), ApiController, Authorize]
     public class TransactionsController : BaseController
    {
          
          private readonly IMapper _mapper;

          public TransactionsController(IMapper mapper)
          {
              _mapper = mapper;
          }

          [HttpGet, ProducesResponseType(typeof(IEnumerable<GetTransactionQueryResult>), StatusCodes.Status200OK), ProducesDefaultResponseType]
          public async Task<IActionResult> GetTransactions()
          {
               var transactions = await Mediator.Send(new GetTransactionsQuery());
               return Ok(transactions);
          }

          [HttpGet("{transactionId}", Name = "GetTransaction"), ProducesResponseType(typeof(GetTransactionQueryResult), StatusCodes.Status200OK), ProducesDefaultResponseType]
          public async Task<IActionResult> GetTransaction(Guid transactionId)
          {
               var transaction = await Mediator.Send(new GetTransactionQuery { Id = transactionId });

               return Ok(transaction);
          }

          [HttpPost(Name = "CreateTransaction"), ProducesResponseType(typeof(GetTransactionQueryResult), StatusCodes.Status201Created), ProducesDefaultResponseType]
          public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionCommand command)
          {
               var result = await Mediator.Send(command);
               var transaction = _mapper.Map<GetTransactionQueryResult>(result);
               return CreatedAtRoute("GetTransaction", new { transactionId = result.Id }, transaction);
          }

          [HttpPut(Name = "UpdateTransaction"), ProducesResponseType(typeof(GetTransactionQueryResult), StatusCodes.Status201Created), ProducesDefaultResponseType]
          public async Task<IActionResult> UpdateTransaction([FromBody] UpdateTransactionCommand command)
          {
               var result = await Mediator.Send(command);
               var transaction = _mapper.Map<GetTransactionQueryResult>(result);
               return AcceptedAtRoute("GetTransaction", new { transactionId = result.Id }, transaction);
          }

          [HttpDelete(Name = "DeleteTransaction"), ProducesResponseType(typeof(DeleteCommandResult), StatusCodes.Status201Created), ProducesResponseType(typeof(string), StatusCodes.Status404NotFound), ProducesDefaultResponseType]
          public async Task<IActionResult> DeleteTransaction([FromBody] DeleteTransactionCommand command)
          {
               var result = await Mediator.Send(command);
               if (!result.IsDeleted)
               {
                    return NotFound($"{result.Message}");
               }
               return NoContent();
          }

          [HttpGet("{userId}/transactions", Name = "GetTransactionsForUser"), ProducesResponseType(typeof(IEnumerable<GetTransactionQueryResult>), StatusCodes.Status200OK), ProducesDefaultResponseType]
          public async Task<IActionResult> GetTransactionsForUser(string userId)
          {
               var transactions = await Mediator.Send(new GetTransactionsForUserQuery { AppUserId = userId });
               return Ok(transactions);
          }
     }
}