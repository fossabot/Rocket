﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.ObjectBuilder2;
using Rocket.API.Configuration;
using Rocket.API.DependencyInjection;
using Rocket.API.Permissions;
using Rocket.API.User;
using Rocket.Core.ServiceProxies;

namespace Rocket.Core.Permissions
{
    public class PermissionProviderProxy : ServiceProxy<IPermissionProvider>, IPermissionProvider
    {
        public PermissionProviderProxy(IDependencyContainer container) : base(container) { }

        public IEnumerable<string> GetGrantedPermissions(IPermissionEntity target, bool inherit = true)
        {
            return ProxiedServices
                   .Where(c => c.SupportsTarget(target))
                   .SelectMany(c => c.GetGrantedPermissions(target, inherit));
        }

        public IEnumerable<string> GetDeniedPermissions(IPermissionEntity target, bool inherit = true)
        {
            return ProxiedServices
                   .Where(c => c.SupportsTarget(target))
                   .SelectMany(c => c.GetDeniedPermissions(target, inherit));
        }

        public bool SupportsTarget(IPermissionEntity target)
        {
            return ProxiedServices.Any(c => c.SupportsTarget(target));
        }

        public PermissionResult CheckPermission(IPermissionEntity target, string permission)
        {
            GuardTarget(target);

            foreach (IPermissionProvider provider in ProxiedServices.Where(c => c.SupportsTarget(target)))
            {
                PermissionResult result = provider.CheckPermission(target, permission);
                if (result == PermissionResult.Default)
                    continue;

                return result;
            }

            return PermissionResult.Default;
        }

        public PermissionResult CheckHasAllPermissions(IPermissionEntity target, params string[] permissions)
        {
            GuardTarget(target);

            foreach (IPermissionProvider provider in ProxiedServices.Where(c => c.SupportsTarget(target)))
            {
                PermissionResult result = provider.CheckHasAllPermissions(target, permissions);
                if (result == PermissionResult.Default)
                    continue;

                return result;
            }

            return PermissionResult.Default;
        }

        public PermissionResult CheckHasAnyPermission(IPermissionEntity target, params string[] permissions)
        {
            GuardTarget(target);

            foreach (IPermissionProvider provider in ProxiedServices.Where(c => c.SupportsTarget(target)))
            {
                PermissionResult result = provider.CheckHasAnyPermission(target, permissions);
                if (result == PermissionResult.Default)
                    continue;

                return result;
            }

            return PermissionResult.Default;
        }

        public bool AddPermission(IPermissionEntity target, string permission)
            => throw new NotSupportedException("Adding permissions from proxy is not supported.");

        public bool AddDeniedPermission(IPermissionEntity target, string permission)
            => throw new NotSupportedException("Adding inverted permissions from proxy is not supported.");

        public bool RemovePermission(IPermissionEntity target, string permission)
            => throw new NotSupportedException("Removing permissions from proxy is not supported.");

        public bool RemoveDeniedPermission(IPermissionEntity target, string permission)
            => throw new NotSupportedException("Removing inverted permissions from proxy is not supported.");

        public IPermissionGroup GetPrimaryGroup(IUser user)
        {
            IPermissionGroup group;
            foreach (IPermissionProvider service in ProxiedServices.Where(c => c.SupportsTarget(user)))
                if ((group = service.GetPrimaryGroup(user)) != null)
                    return group;

            return null;
        }

        public IPermissionGroup GetGroup(string id)
        {
            return GetGroups().FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<IPermissionGroup> GetGroups(IPermissionEntity target)
        {
            return ProxiedServices.Where(c => c.SupportsTarget(target))
                                  .SelectMany(c => c.GetGroups(target));
        }

        public IEnumerable<IPermissionGroup> GetGroups()
        {
            return ProxiedServices.SelectMany(c => c.GetGroups());
        }

        public bool UpdateGroup(IPermissionGroup group)
            => throw new NotSupportedException("Updating groups from proxy is not supported.");

        public bool AddGroup(IPermissionEntity target, IPermissionGroup group)
            => throw new NotSupportedException("Adding groups from proxy is not supported.");

        public bool RemoveGroup(IPermissionEntity target, IPermissionGroup group)
            => throw new NotSupportedException("Removing groups from proxy is not supported.");

        public bool CreateGroup(IPermissionGroup group)
            => throw new NotSupportedException("Creating groups from proxy is not supported.");

        public bool DeleteGroup(IPermissionGroup group)
            => throw new NotSupportedException("Deleting groups from proxy is not supported.");

        public void Load(IConfigurationContext context)
        {
            ProxiedServices.ForEach(c => c.Load(context));
        }

        public void Reload()
        {
            ProxiedServices.ForEach(c => c.Reload());
        }

        public void Save()
        {
            ProxiedServices.ForEach(c => c.Save());
        }

        private void GuardTarget(IPermissionEntity target)
        {
            if (!SupportsTarget(target))
                throw new NotSupportedException(target.GetType().FullName + " is not supported!");
        }

        public string ServiceName => "ProxyPermissions";
    }
}