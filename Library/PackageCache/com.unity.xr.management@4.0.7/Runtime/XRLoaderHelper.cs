using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace UnityEngine.XR.Management
{
    /// <summary>
    /// XR Loader abstract subclass used as a base class for specific provider implementations. Class provides some
    /// helper logic that can be used to handle subsystem handling in a typesafe manner, reducing potential boilerplate
    /// code.
    /// </summary>
    public abstract class XRLoaderHelper : XRLoader
    {
        /// <summary>
        /// Map of loaded susbsystems. Used so we don't always have to fo to XRSubsystemManger and do a manual
        /// search to find the instance we loaded.
        /// </summary>
        protected Dictionary<Type, ISubsystem> m_SubsystemInstanceMap = new Dictionary<Type, ISubsystem>();

        /// <summary>
        /// Gets the loaded subsystem of the specified type. Implementation dependent as only implemetnations
        /// know what they have loaded and how best to get it..
        /// </summary>
        ///
        /// <typeparam name="T">Type of the subsystem to get.</typeparam>
        ///
        /// <returns>The loaded subsystem or null if not found.</returns>
        public override T GetLoadedSubsystem<T>()
        {
            Type subsystemType = typeof(T);
            ISubsystem subsystem;
            m_SubsystemInstanceMap.TryGetValue(subsystemType, out subsystem);
            return subsystem as T;
        }

        /// <summary>
        /// Start a subsystem instance of a given type. Subsystem assumed to already be loaded from
        /// a previous call to CreateSubsystem
        /// </summary>
        ///
        /// <typeparam name="T">A subclass of <see cref="ISubsystem"/></typeparam>
        protected void StartSubsystem<T>() where T : class, ISubsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
                subsystem.Start();
        }

        /// <summary>
        /// Stop a subsystem instance of a given type. Subsystem assumed to already be loaded from
        /// a previous call to CreateSubsystem
        /// </summary>
        ///
        /// <typeparam name="T">A subclass of <see cref="ISubsystem"/></typeparam>
        protected void StopSubsystem<T>() where T : class, ISubsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
                subsystem.Stop();
        }

        /// <summary>
        /// Destroy a subsystem instance of a given type. Subsystem assumed to already be loaded from
        /// a previous call to CreateSubsystem
        /// </summary>
        ///
        /// <typeparam name="T">A subclass of <see cref="ISubsystem"/></typeparam>
        protected void DestroySubsystem<T>() where T : class, ISubsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
            {
                var subsystemType = typeof(T);
                if (m_SubsystemInstanceMap.ContainsKey(subsystemType))
                    m_SubsystemInstanceMap.Remove(subsystemType);
                subsystem.Destroy();
            }
        }

        /// <summary>
        /// Creates a subsystem given a list of descriptors and a specific subsystem id.
        ///
        /// You should make sure to destroy any subsystem that you created so that resources
        /// acquired by your subsystems are correctly cleaned up and released. This is especially important
        /// if you create them during initialization, but initialization fails. If that happens,
        /// you should clean up any subsystems created up to that point.
        /// </summary>
        ///
        /// <typeparam name="TDescriptor">The descriptor type being passed in.</typeparam>
        /// <typeparam name="TSubsystem">The subsystem type being requested</typeparam>
        /// <param name="descriptors">List of TDescriptor instances to use for subsystem matching.</param>
        /// <param name="id">The identifier key of the particualr subsystem implementation being requested.</param>
        protected void CreateSubsystem<TDescriptor, TSubsystem>(List<TDescriptor> descriptors, string id)
            where TDescriptor : ISubsystemDescriptor
            where TSubsystem : ISubsystem
        {
            if (descriptors == null)
                throw new ArgumentNullException("descriptors");

            SubsystemManager.GetSubsystemDescriptors<TDescriptor>(descriptors);

            if (descriptors.Count > 0)
            {
                foreach (var descriptor in descriptors)
                {
                    ISubsystem subsys = null;
                    if (String.Compare(descriptor.id, id, true) == 0)
                    {
                        subsys = descriptor.Create();
                    }
                    if (subsys != null)
                    {
                        m_SubsystemInstanceMap[typeof(TSubsystem)] = subsys;
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Creates a native, integrated subsystem given a list of descriptors and a specific subsystem id.
        /// DEPRECATED: Please use the geenric CreateSubsystem method. This method is soley retained for
        /// backwards compatibility and will be removed in a future release.
        /// </summary>
        ///
        /// <typeparam name="TDescriptor">The descriptor type being passed in.</typeparam>
        /// <typeparam name="TSubsystem">The subsystem type being requested</typeparam>
        /// <param name="descriptors">List of TDescriptor instances to use for subsystem matching.</param>
        /// <param name="id">The identifier key of the particualr subsystem implementation being requested.</param>
        [Obsolete("This method is obsolete. Please use the geenric CreateSubsystem method.", false)]
        protected void CreateIntegratedSubsystem<TDescriptor, TSubsystem>(List<TDescriptor> descriptors, string id)
            where TDescriptor : IntegratedSubsystemDescriptor
            where TSubsystem : IntegratedSubsystem
        {
            CreateSubsystem<TDescriptor, TSubsystem>(descriptors, id);
        }

        /// <summary>
        /// Creates a managed, standalone subsystem given a list of descriptors and a specific subsystem id.
        /// DEPRECATED: Please use the geenric CreateSubsystem method. This method is soley retained for
        /// backwards compatibility and will be removed in a future release.
        /// </summary>
        ///
        /// <typeparam name="TDescriptor">The descriptor type being passed in.</typeparam>
        /// <typeparam name="TSubsystem">The subsystem type being requested</typeparam>
        /// <param name="descriptors">List of TDescriptor instances to use for subsystem matching.</param>
        /// <param name="id">The identifier key of the particualr subsystem implementation being requested.</param>
        [Obsolete("This method is obsolete. Please use the generic CreateSubsystem method.", false)]
        protected void CreateStandaloneSubsystem<TDescriptor, TSubsystem>(List<TDescriptor> descriptors, string id)
            where TDescriptor : SubsystemDescriptor
            where TSubsystem : Subsystem
        {
            CreateSubsystem<TDescriptor, TSubsystem>(descriptors, id);
        }

        /// <summary>
        /// Override of <see cref="XRLoader.Deinitialize"/> to provide for clearing the instance map.true
        ///
        /// If you override this method in your subclass, you must call the base
        /// implementation to allow the instance map tp be cleaned up correctly.
        /// </summary>
        ///
        /// <returns>True if de-initialization was successful.</returns>
        public override bool Deinitialize()
        {
            m_SubsystemInstanceMap.Clear();
            return base.Deinitialize();
        }

#if UNITY_EDITOR
        virtual public void WasAssignedToBuildTarget(BuildTargetGroup buildTargetGroup)
        {

        }

        virtual public void WasUnassignedFromBuildTarget(BuildTargetGroup buildTargetGroup)
        {

        }
#endif
    }
}
