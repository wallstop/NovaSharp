namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Common interface for all resources which are uniquely bound to a script.
    /// </summary>
    public interface IScriptPrivateResource
    {
        /// <summary>
        /// Gets the script owning this resource.
        /// </summary>
        /// <value>
        /// The script owning this resource.
        /// </value>
        public Script OwnerScript { get; }
    }

    /// <summary>
    /// Helper methods enforcing that script-private resources (tables, coroutines, etc.) do not cross script boundaries.
    /// </summary>
    internal static class ScriptPrivateResourceExtension
    {
        /// <summary>
        /// Gets the single script intrinsically owning a value, including nested tuple members.
        /// </summary>
        internal static Script GetOwnerScript(this DynValue value)
        {
            if (value.Type != DataType.Tuple)
            {
                return value.ScriptPrivateResource?.OwnerScript;
            }

            DynValue[] tuple = value.Tuple;
            if (tuple == null)
            {
                return null;
            }

            for (int i = 0; i < tuple.Length; i++)
            {
                if (tuple[i].Type == DataType.Tuple)
                {
                    return GetNestedTupleOwner(tuple);
                }
            }

            Script owner = null;
            for (int i = 0; i < tuple.Length; i++)
            {
                MergeOwner(tuple[i], ref owner);
            }

            return owner;
        }

        private static Script GetNestedTupleOwner(DynValue[] root)
        {
            Script owner = null;
            using (HashSetPool<DynValue[]>.Get(out HashSet<DynValue[]> visited))
            using (ListPool<DynValue[]>.Get(out List<DynValue[]> pending))
            {
                pending.Add(root);
                while (pending.Count > 0)
                {
                    int last = pending.Count - 1;
                    DynValue[] tuple = pending[last];
                    pending.RemoveAt(last);
                    if (tuple == null || !visited.Add(tuple))
                    {
                        continue;
                    }

                    for (int i = 0; i < tuple.Length; i++)
                    {
                        DynValue value = tuple[i];
                        if (value.Type == DataType.Tuple)
                        {
                            pending.Add(value.Tuple);
                        }
                        else
                        {
                            MergeOwner(value, ref owner);
                        }
                    }
                }
            }

            return owner;
        }

        private static void MergeOwner(DynValue value, ref Script owner)
        {
            Script candidate = value.ScriptPrivateResource?.OwnerScript;
            if (candidate == null)
            {
                return;
            }

            if (owner != null && !ReferenceEquals(owner, candidate))
            {
                throw new ScriptRuntimeException(
                    "Attempt to perform operations with resources owned by different scripts."
                );
            }

            owner = candidate;
        }

        /// <summary>
        /// Ensures every DynValue in the array belongs to the same script as the containing resource.
        /// </summary>
        public static void CheckScriptOwnership(
            this IScriptPrivateResource containingResource,
            DynValue[] values
        )
        {
            foreach (DynValue v in values)
            {
                CheckScriptOwnership(containingResource, v);
            }
        }

        /// <summary>
        /// Ensures every DynValue in the span belongs to the same script as the containing resource.
        /// </summary>
        public static void CheckScriptOwnership(
            this IScriptPrivateResource containingResource,
            ReadOnlySpan<DynValue> values
        )
        {
            for (int i = 0; i < values.Length; i++)
            {
                CheckScriptOwnership(containingResource, values[i]);
            }
        }

        /// <summary>
        /// Ensures the provided DynValue is safe to use within the containing resource's script.
        /// </summary>
        public static void CheckScriptOwnership(
            this IScriptPrivateResource containingResource,
            DynValue value
        )
        {
            if (value.Type == DataType.Tuple)
            {
                CheckTupleOwnership(containingResource, value.Tuple);
                return;
            }

            IScriptPrivateResource otherResource = value.ScriptPrivateResource;

            if (otherResource != null)
            {
                CheckScriptOwnership(containingResource, otherResource);
            }
        }

        private static void CheckTupleOwnership(
            IScriptPrivateResource containingResource,
            DynValue[] values
        )
        {
            if (values == null)
            {
                return;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].Type == DataType.Tuple)
                {
                    CheckNestedTupleOwnership(containingResource, values);
                    return;
                }
            }

            for (int i = 0; i < values.Length; i++)
            {
                DynValue value = values[i];
                IScriptPrivateResource otherResource = value.ScriptPrivateResource;
                if (otherResource != null)
                {
                    CheckScriptOwnership(containingResource, otherResource);
                }
            }
        }

        private static void CheckNestedTupleOwnership(
            IScriptPrivateResource containingResource,
            DynValue[] root
        )
        {
            using (HashSetPool<DynValue[]>.Get(out HashSet<DynValue[]> visited))
            using (ListPool<DynValue[]>.Get(out List<DynValue[]> pending))
            {
                pending.Add(root);
                while (pending.Count > 0)
                {
                    int last = pending.Count - 1;
                    DynValue[] values = pending[last];
                    pending.RemoveAt(last);
                    if (values == null || !visited.Add(values))
                    {
                        continue;
                    }

                    for (int i = 0; i < values.Length; i++)
                    {
                        DynValue value = values[i];
                        if (value.Type == DataType.Tuple)
                        {
                            pending.Add(value.Tuple);
                            continue;
                        }

                        IScriptPrivateResource otherResource = value.ScriptPrivateResource;
                        if (otherResource != null)
                        {
                            CheckScriptOwnership(containingResource, otherResource);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates that the given script matches the resource's owner when crossing API boundaries.
        /// </summary>
        public static void CheckScriptOwnership(this IScriptPrivateResource resource, Script script)
        {
            if (resource.OwnerScript != null && resource.OwnerScript != script && script != null)
            {
                throw new ScriptRuntimeException(
                    "Attempt to access a resource owned by a script, from another script"
                );
            }
        }

        /// <summary>
        /// Compares two resources and throws when they belong to different scripts or when a script-bound item is used by a shared resource.
        /// </summary>
        public static void CheckScriptOwnership(
            this IScriptPrivateResource containingResource,
            IScriptPrivateResource itemResource
        )
        {
            if (itemResource != null)
            {
                if (
                    containingResource.OwnerScript != null
                    && containingResource.OwnerScript != itemResource.OwnerScript
                    && itemResource.OwnerScript != null
                )
                {
                    throw new ScriptRuntimeException(
                        "Attempt to perform operations with resources owned by different scripts."
                    );
                }
                else if (containingResource.OwnerScript == null && itemResource.OwnerScript != null)
                {
                    throw new ScriptRuntimeException(
                        "Attempt to perform operations with a script private resource on a shared resource."
                    );
                }
            }
        }
    }
}
