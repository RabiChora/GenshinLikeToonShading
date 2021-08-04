
namespace UnityEngine.XR.Management.Tests.Standalone
{
    namespace Providing
    {
        public class StandaloneSubsystemParams
        {
            public string id { get; set;}
            public System.Type subsystemImplementationType { get; set; }

            public StandaloneSubsystemParams(string id, System.Type subsystemImplType)
            {
                this.id = id;
                this.subsystemImplementationType = subsystemImplType;
            }
        }
    }

    public class StandaloneSubsystemDescriptor : SubsystemDescriptor<StandaloneSubsystem>
    {
        public static void Create(Providing.StandaloneSubsystemParams parms)
        {
            SubsystemRegistration.CreateDescriptor(new StandaloneSubsystemDescriptor(parms.id, parms.subsystemImplementationType));
        }

        public StandaloneSubsystemDescriptor(string id, System.Type subsystemImplType)
        {
            this.id = id;
            this.subsystemImplementationType = subsystemImplType;
        }
    }
}
