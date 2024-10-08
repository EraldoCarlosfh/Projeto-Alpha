﻿using Alpha.Framework.MediatR.Data.UnitOfWork;
using Alpha.Framework.MediatR.EventSourcing.Aggregates;
using Alpha.Framework.MediatR.EventSourcing.Entity;
using Alpha.Framework.MediatR.Notifications;
using Alpha.Framework.MediatR.SecurityService.Models;
using MediatR;
using Newtonsoft.Json;
using System.Data.Entity.Core.Common.CommandTrees;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alpha.Framework.MediatR.Auditorship.Domain.Entities;
using Alpha.Framework.MediatR.Auditorship.Domain.Enums;
using System.Reflection;

namespace Alpha.Framework.MediatR.EventSourcing.Commands
{
    public abstract class CommandHandlerBase<TEntity, TRequest, TResult, TAggregate> : IRequestHandler<TRequest, TResult>
        where TRequest : IRequest<TResult>
        where TEntity : Entity<TEntity>
        where TResult : class
        where TAggregate : AggregateBase<TEntity>
    {
        protected readonly AuthenticatedUserModel _authenticatedUser;
        private readonly IUnitOfWork _unitOfWork;

        public CommandHandlerBase(IUnitOfWork unitOfWork, AuthenticatedUserModel authenticatedUser)
        {
            _authenticatedUser = authenticatedUser;
            _unitOfWork = unitOfWork;
        }

        public async Task<TResult> Handle(TRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var aggregate = Activator.CreateInstance(typeof(TAggregate)) as TAggregate;

                _unitOfWork.BeginTransaction();

                var result = await HandleRequest(request, aggregate, cancellationToken);

                var audit = Audit.New();
                audit.EntityId = GetEntityId(result);
                audit.EntityClassIdentification = typeof(TEntity).Name;
                audit.OperationType = OperationType.GetByRequestName(request.GetType().Name);
                audit.RequestClassIdentification = request.GetType().Name;
                audit.RequestPayload = JsonConvert.SerializeObject(request, new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                });
                audit.UserIdentification = _authenticatedUser.Email ?? _authenticatedUser.Id;
                await _unitOfWork.AddAudit(audit);

                await _unitOfWork.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        protected async Task<int> CommitPendingChanges()
        {
            return await _unitOfWork.ForceCommitAsync();
        }

        private EntityId GetEntityId(TResult result)
        {
            if (result == null) return null;
            var properties = result.GetType().GetProperties();
            var idProperty = properties.FirstOrDefault(c => c.Name == "Id");

            if (idProperty != null)
            {
                var idValue = idProperty.GetValue(result).ToString();

                return new EntityId(idValue);
            }

            return null;
        }

        protected abstract Task<TResult> HandleRequest(TRequest request, TAggregate aggregate, CancellationToken cancellationToken);
    }


    public abstract class CommandHandlerBase<TEntity, TRequest, TResult> : OctaNotifiable, IRequestHandler<TRequest, TResult>
       where TRequest : IRequest<TResult>
       where TEntity : Entity<TEntity>
       where TResult : class
    {
        protected readonly AuthenticatedUserModel _authenticatedUser;
        private readonly IUnitOfWork _unitOfWork;

        public CommandHandlerBase(IUnitOfWork unitOfWork, AuthenticatedUserModel authenticatedUser)
        {
            _authenticatedUser = authenticatedUser;
            _unitOfWork = unitOfWork;
        }

        public async Task<TResult> Handle(TRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var result = await HandleRequest(request, cancellationToken);

                if (result != null)
                {
                    var audit = Audit.New();
                    audit.EntityId = GetEntityId(result);
                    audit.EntityClassIdentification = typeof(TEntity).Name;
                    audit.OperationType = OperationType.GetByRequestName(request.GetType().Name);
                    audit.RequestClassIdentification = request.GetType().Name;
                    audit.RequestPayload = JsonConvert.SerializeObject(request, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });

                    audit.UserIdentification = _authenticatedUser.Email ?? _authenticatedUser.Id;
                    await _unitOfWork.AddAudit(audit);

                    Type type = result.GetType();
                    PropertyInfo prop = type.GetProperty("IsSuccess");
                    var success = (bool)prop.GetValue(result);

                    if (success)
                    {
                        await _unitOfWork.CommitAsync();
                    }
                    else
                    {
                        await _unitOfWork.CommitAsyncForDbSet<Audit>();
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        protected async Task<int> CommitPendingChanges()
        {
            return await _unitOfWork.ForceCommitAsync();
        }

        private EntityId GetEntityId(TResult result)
        {
            if (result == null) return null;
            var properties = result.GetType().GetProperties();
            var idProperty = properties.FirstOrDefault(c => c.Name == "Id");

            if (idProperty != null)
            {
                var idValue = idProperty.GetValue(result).ToString();

                return new EntityId(idValue);
            }

            return null;
        }

        protected abstract Task<TResult> HandleRequest(TRequest request, CancellationToken cancellationToken);

    }
}
