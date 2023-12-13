using System.Threading.Tasks;
using Function.Domain.Models.OL;

namespace Function.Domain.Helpers
{
    public class OlSynapseMessageConsolidation : IOlMessageConsolidation
    {
        public Task<Event?> ConsolidateEvent(Event olEvent, string runId)
        {
            throw new System.NotImplementedException();        
        }

        // ProcessStart
        // ProcessComplete
        // CaptureEvent (start and complete)
        // ShouldConsolidateMessage    
        // GetCapturedEvents
        // ConsolidateEvents

        /*
        * 1. If message is start or complete capture it.
        * 2. Get list of captured events.
        * 3. Try to consolidate captured events.
        *   3.1. Check for unique list of inputs and outputs.
        * 4. If consolidation is successful, return consolidated event.

        - Consolidated event should only CreateOrUpdate in Purview. When augmenting, it should append to the existing asset.
        - At what point should we enrich the event with data from Purview? We want to merge the data from Purview with the data from OL.
        - If an event is consolidated, it should be cached in memory to avoid duplicate consolidation. 
          Future messages may continue to arrive and trigger consolidation but nothing has changed. 
          Eventually a change in the inputs/outputs will be noticed and will trigger a new consolidation.
        - Given the pattern of consolidation returning null if nothing should be processed, this instance can leverage a cache provider to store
            the consolidated event. This will allow the next message to be skipped without having to re-consolidate the event.
        - Cache expiration can be weeks or months as the data will not change.
            - How large will this cache get?
        - Consider stripping the Spark Logic Plan from the event as this can be quite large and not used.

        
        */

        /*
flowchart TD
    A[Message handler] --> B(CheckMessageStatus)
    B --> C{IsStart}
    C --> |No| D(CheckIfEnoughData)
    C --> |Yes| E(CheckIfExists)    
    E --> |Yes| F(Skip)
    E --> |No| G(Log)
    D --> |No| H(Log)
    D --> D1(Consolidate SC</br>I/O)
    D1 --> |Yes| I(CreateOrUpdatePurviewAsset)
    H --> Z[END]
    G --> Z[END]
    F --> Z[END]
    I --> Z[END]        
        */

    }    
}