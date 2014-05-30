﻿namespace TopHat.CodeGeneration {
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;

    using TopHat.Engine;

    public interface IGeneratedCodeManager {
        /// <summary>
        ///     Returns a reference to the generated code assembly
        /// </summary>
        Assembly GeneratedCodeAssembly { get; }

        /// <summary>
        ///     Loads the Generated code for this project
        /// </summary>
        /// <param name="config"></param>
        /// <remarks>This method does not generate code nor check for updates</remarks>
        void LoadCode(CodeGeneratorConfig config);

        /// <summary>
        ///     Returns a type for a Foreign Key class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Type GetForeignKeyType<T>();

        /// <summary>
        ///     Returns a type for a Tracked class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Type GetTrackingType<T>();

        /// <summary>
        ///     Returns an instance of a Foreign Key class for the type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T CreateForeignKeyInstance<T>();

        /// <summary>
        ///     Returns an instance of a tracking class for the type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T CreateTrackingInstance<T>();

        /// <summary>
        ///     Convenience method for enabling tracking on a tracking instance
        /// </summary>
        void TrackInstance<T>(T entity);

        /// <summary>
        /// Calls the correct static function within the generated code for a particular fetch tree
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <param name="query"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(SqlWriterResult result, SelectQuery<T> query, IDbConnection conn);
    }
}