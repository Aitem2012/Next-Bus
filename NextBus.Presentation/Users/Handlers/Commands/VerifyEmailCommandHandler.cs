﻿using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Users.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Users.Handlers.Commands
{
    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, bool>
     {
          private readonly IAppDbContext _context;
          private readonly IMapper _mapper;
          private readonly IMediator _mediator;
          private readonly UserManager<Domain.Users.AppUser> _userManager;

          public VerifyEmailCommandHandler(IAppDbContext context, IMapper mapper, IMediator mediator, UserManager<Domain.Users.AppUser> userManager)
          {
               _context = context;
               _mapper = mapper;
               _mediator = mediator;
               _userManager = userManager;
          }

          public async Task<bool> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
          {
               var user = await _context.AppUsers.SingleAsync(e => e.UserName.Equals(request.Username),
                    cancellationToken);

               return user != null && _userManager.ConfirmEmailAsync(user, request.Token).Result.Succeeded;
          }
     }
}
