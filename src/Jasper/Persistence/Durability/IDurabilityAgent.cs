using System.Threading.Tasks;

namespace Jasper.Persistence.Durability;

public interface IDurabilityAgent
{
    void EnqueueLocally(Envelope envelope);
    void RescheduleIncomingRecovery();
    void RescheduleOutgoingRecovery();
}
