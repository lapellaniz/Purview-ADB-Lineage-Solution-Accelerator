# Troubleshoot OpenLineage Message Handling

Go to Application Insights `search` feature and using the text provided below, debug the application.

## OpenLineageIn

Azure Function that handles the incoming API call from Spark pool with the OpenLineage message.

- View JSON OpenLineage message sent from Spark pool
    - `OpenLineageIn: <<`
- OpenLineage message was skipped
    - `OpenLineageIn: Request will be skipped.`    

## PurviewOut

Azure Function that handles the outgoing API call to send messages to Purview.

- View assets parsed and to be sent to Purview
    - `PurviewOut-ParserService:`
- View JSON sent to Purview
    - `Sending this payload to Purview:`

## Log Entries

- Confirm Function has loaded
    - `Generating 2 job function(s)`
    - `Job host started`

- Query Log Analytics Spark event data and confirm OL logs
    ```sql
    SparkLoggingEvent_CL 
    | where clusterName_s == "lratest"
    | where applicationName_s has "1702401190"
    | where logger_name_s startswith "io.openlineage"
    ```