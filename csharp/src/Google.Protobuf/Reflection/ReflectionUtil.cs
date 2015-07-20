#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.  All rights reserved.
// https://developers.google.com/protocol-buffers/
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Google.Protobuf.Reflection
{
    /// <summary>
    /// The methods in this class are somewhat evil, and should not be tampered with lightly.
    /// Basically they allow the creation of relatively weakly typed delegates from MethodInfos
    /// which are more strongly typed. They do this by creating an appropriate strongly typed
    /// delegate from the MethodInfo, and then calling that within an anonymous method.
    /// Mind-bending stuff (at least to your humble narrator) but the resulting delegates are
    /// very fast compared with calling Invoke later on.
    /// </summary>
    internal static class ReflectionUtil
    {
        /// <summary>
        /// Empty Type[] used when calling GetProperty to force property instead of indexer fetching.
        /// </summary>
        internal static readonly Type[] EmptyTypes = new Type[0];

        /// <summary>
        /// Creates a delegate which will cast the argument to the appropriate method target type,
        /// call the method on it, then convert the result to object.
        /// </summary>
        internal static Func<object, object> CreateFuncObjectObject(MethodInfo method)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(object), "p");
            Expression downcast = Expression.Convert(parameter, method.DeclaringType);
            Expression call = Expression.Call(downcast, method);
            Expression upcast = Expression.Convert(call, typeof(object));
            return Expression.Lambda<Func<object, object>>(upcast, parameter).Compile();
        }

        /// <summary>
        /// Creates a delegate which will cast the argument to the appropriate method target type,
        /// call the method on it, then convert the result to the specified type.
        /// </summary>
        internal static Func<object, T> CreateFuncObjectT<T>(MethodInfo method)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(object), "p");
            Expression downcast = Expression.Convert(parameter, method.DeclaringType);
            Expression call = Expression.Call(downcast, method);
            Expression upcast = Expression.Convert(call, typeof(T));
            return Expression.Lambda<Func<object, T>>(upcast, parameter).Compile();
        }

        /// <summary>
        /// Creates a delegate which will execute the given method after casting the first argument to
        /// the target type of the method, and the second argument to the first parameter type of the method.
        /// </summary>
        internal static Action<object, object> CreateActionObjectObject(MethodInfo method)
        {
            ParameterExpression targetParameter = Expression.Parameter(typeof(object), "target");
            ParameterExpression argParameter = Expression.Parameter(typeof(object), "arg");
            Expression castTarget = Expression.Convert(targetParameter, method.DeclaringType);
            Expression castArgument = Expression.Convert(argParameter, method.GetParameters()[0].ParameterType);
            Expression call = Expression.Call(castTarget, method, castArgument);
            return Expression.Lambda<Action<object, object>>(call, targetParameter, argParameter).Compile();
        }

        /// <summary>
        /// Creates a delegate which will execute the given method after casting the first argument to
        /// the target type of the method.
        /// </summary>
        internal static Action<object> CreateActionObject(MethodInfo method)
        {
            ParameterExpression targetParameter = Expression.Parameter(typeof(object), "target");
            Expression castTarget = Expression.Convert(targetParameter, method.DeclaringType);
            Expression call = Expression.Call(castTarget, method);
            return Expression.Lambda<Action<object>>(call, targetParameter).Compile();
        }

        /// <summary>
        /// Returns the next type from an iterator of types, unless the iterator is a null reference,
        /// in which case null is returned.
        /// </summary>
        internal static Type GetNextType(IEnumerator<Type> generatedTypeIterator)
        {
            if (generatedTypeIterator == null)
            {
                return null;
            }
            if (!generatedTypeIterator.MoveNext())
            {
                // This parameter name corresponds to any public method supplying the generated types to start with.
                throw new ArgumentException("More generated types left over after consuming all expected ones", "generatedTypes");
            }
            return generatedTypeIterator.Current;
        }

    }
}