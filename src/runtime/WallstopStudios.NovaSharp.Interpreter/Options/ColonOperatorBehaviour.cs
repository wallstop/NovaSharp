namespace WallstopStudios.NovaSharp.Interpreter.Options
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Defines behaviour of the colon ':' operator in CLR callbacks.
    /// Default behaviour is for ':' being treated the same as a '.' if the functions is implemented on the CLR side (e.g. in C#).
    /// </summary>
    public enum ColonOperatorBehaviour
    {
        /// <summary>
        /// The colon is treated the same as the dot ('.') operator.
        /// </summary>
        [Obsolete("Use an explicit colon operator behaviour.", false)]
        Unknown = 0,
        TreatAsDot = 1,

        /// <summary>
        /// The colon is treated the same as the dot ('.') operator if the first argument is userdata, as a Lua colon operator otherwise.
        /// </summary>
        TreatAsDotOnUserData = 2,

        /// <summary>
        /// The colon is treated in the same as the Lua colon operator works.
        /// </summary>
        TreatAsColon = 3,
    }
}
