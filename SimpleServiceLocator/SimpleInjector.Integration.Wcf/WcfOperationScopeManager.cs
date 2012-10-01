﻿#region Copyright (c) 2012 S. van Deursen
/* The Simple Injector is an easy-to-use Inversion of Control library for .NET
 * 
 * Copyright (C) 2012 S. van Deursen
 * 
 * To contact me, please visit my blog at http://www.cuttingedge.it/blogs/steven/ or mail to steven at 
 * cuttingedge.it.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
 * associated documentation files (the "Software"), to deal in the Software without restriction, including 
 * without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the 
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial 
 * portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
 * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO 
 * EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE 
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

namespace SimpleInjector.Integration.Wcf
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    // This class will be registered as singleton within a container, allowing each container (if the
    // application -for some reason- has multiple containers) to have it's own set of lifetime scopes, without
    // influencing other scopes from other containers.
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "A WcfOperationScopeManager instance is stored as singleton in a Container instance, " +
            "but the container itself does not implement IDisposable, and will never dispose any instances " +
            "it contains. Letting WcfRequestScopeManager implement IDisposable does not help and when the " +
            "application creates multiple Containers (that go out of scope before the AppDomain is stopped), " +
            "we must rely on the garbage collector calling the Finalize method of the ThreadLocal<T>.")]
    internal sealed class WcfOperationScopeManager
    {
        // Here we use .NET 4.0 ThreadLocal instead of the [ThreadStatic] attribute, to allow each container
        // to have it's own set of scopes.
        private readonly ThreadLocal<WcfOperationScope> threadLocalScopes = new ThreadLocal<WcfOperationScope>();

        internal WcfOperationScopeManager(IAutoRegistrationProtection autoWiringProtection)
        {
        }

        // This marker interface protects this manager from being auto-registered in the case
        // where a user overrides the container.Options.ConstructorResolutionBehavior to allow registering 
        // types with internal constructors. Because the user can't register this internal interface, it is
        // very unlikely it will be able to register this manager. Auto-registration is bad, since this
        // manager must be registered as a singleton. A user would still be able to auto-register this type,
        // by overriding the container.Options.ConstructorInjectionBehavior, but this would be a very
        // deliberate action and an unlike situation.
        internal interface IAutoRegistrationProtection
        {
        }

        internal WcfOperationScope CurrentScope
        {
            get { return this.threadLocalScopes.Value; }
        }

        internal WcfOperationScope BeginScope()
        {
            if (this.threadLocalScopes.Value != null)
            {
                throw new InvalidOperationException("WCF Scopes can not be nested.");
            }

            var scope = new WcfOperationScope(this);

            this.threadLocalScopes.Value = scope;

            return scope;
        }

        internal void EndLifetimeScope(WcfOperationScope scope)
        {
            this.threadLocalScopes.Value = null;
        }
    }
}