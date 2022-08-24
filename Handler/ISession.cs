using Sora.Entities.Base;

namespace HinaBot_NeoAspect.Handler
{
    public interface ISession
    {
        public SoraApi Session { get; set; }
    }
}
