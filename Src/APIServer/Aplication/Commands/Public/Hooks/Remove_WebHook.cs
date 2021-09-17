using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Aplication.Payload;
using System.Linq;
using FluentValidation;
using Aplication.Shared.Attributes;
using Microsoft.EntityFrameworkCore;
using APIServer.Persistence;
using APIServer.Domain.Core.Models.WebHooks;
using Shared.Aplication.Interfaces;
using APIServer.Aplication.Shared.Errors;

namespace APIServer.Aplication.Commands.WebHooks {

    /// <summary>
    /// Command for removing hook
    /// </summary>
    [Authorize]
    public class RemoveWebHook : IRequest<RemoveWebHookPayload> {

        /// <summary>WebHook Id </summary>
        public long WebHookId { get; set; }
    }

    /// <summary>
    /// RemoveWebHook Validator
    /// </summary>
    public class RemoveWebHookValidator : AbstractValidator<RemoveWebHook> {

        private readonly IDbContextFactory<ApiDbContext> _factory;

        public RemoveWebHookValidator(IDbContextFactory<ApiDbContext> factory){
            _factory = factory;

            RuleFor(e => e.WebHookId)
            .NotNull()
            .GreaterThan(0);

            RuleFor(e => e.WebHookId)
            .MustAsync(HookExist)
            .WithMessage("Hook was not found");
        }

        public async Task<bool> HookExist(RemoveWebHook request, long id, CancellationToken cancellationToken) {
            
            await using ApiDbContext dbContext = 
                _factory.CreateDbContext();
            
            return await dbContext.WebHooks.AnyAsync(e => e.ID == request.WebHookId);
        }
    }

    /// <summary>
    /// IRemoveWebHookError
    /// </summary>
    public interface IRemoveWebHookError { }

    /// <summary>
    /// RemoveWebHookPayload
    /// </summary>
    public class RemoveWebHookPayload : BasePayload<RemoveWebHookPayload, IRemoveWebHookError> {

        /// <summary>
        /// Removed Hook Id
        /// </summary>
        public long removed_id { get; set; }
    }

    /// <summary>Handler for <c>RemoveWebHook</c> command </summary>
    public class RemoveWebHookHandler : IRequestHandler<RemoveWebHook, RemoveWebHookPayload> {

        /// <summary>
        /// Injected <c>ApiDbContext</c>
        /// </summary>
        private readonly IDbContextFactory<ApiDbContext> _factory;

        /// <summary>
        /// Injected <c>IMediator</c>
        /// </summary>
        private readonly IMediator _mediator;

        /// <summary>
        /// Injected <c>IMediator</c>
        /// </summary>
        private readonly ICurrentUser _current;

        /// <summary>
        /// Main constructor
        /// </summary>
        public RemoveWebHookHandler(
            IDbContextFactory<ApiDbContext> factory,
            IMediator mediator,
            ICurrentUser currentuser) {

            _factory = factory;

            _mediator = mediator;

            _current = currentuser;
        }

        /// <summary>
        /// Command handler for <c>RemoveWebHook</c>
        /// </summary>
        public async Task<RemoveWebHookPayload> Handle(RemoveWebHook request, CancellationToken cancellationToken) {
            
            await using ApiDbContext dbContext = 
                _factory.CreateDbContext();

            WebHook wh = await dbContext.WebHooks
            .TagWith(string.Format("RemoveWebHook Command - Query Hook"))
            .Where(e => e.ID == request.WebHookId)
            .FirstOrDefaultAsync(cancellationToken);

            if (wh == null) {
                return RemoveWebHookPayload.Error(new WebHookNotFound());
            }

            long removed_id = wh.ID;

            dbContext.WebHooks.Remove(wh);

            await dbContext.SaveChangesAsync(cancellationToken);

            var response = RemoveWebHookPayload.Success();

            response.removed_id = removed_id;

            return response;
        }
    }
}