namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
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
        /// Ensures the provided DynValue is safe to use within the containing resource's script.
        /// </summary>
        public static void CheckScriptOwnership(
            this IScriptPrivateResource containingResource,
            DynValue value
        )
        {
            if (value != null)
            {
                IScriptPrivateResource otherResource = value.ScriptPrivateResource;

                if (otherResource != null)
                {
                    CheckScriptOwnership(containingResource, otherResource);
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
