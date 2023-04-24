using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides tracking, storage, and export of any data throughout a maze run
/// </summary>
public class TelemetryManager : MonoBehaviour
{
    public static TelemetryManager instance;

    /// <summary>
    /// Base data container that all data classes must inherit from
    /// </summary>
    public abstract class DataContainer { };

    /// <summary>
    /// A function that returns a data class to be stored in the telemetry manager
    /// </summary>
    public delegate DataContainer PollingFunction();
    /// <summary>
    /// All the data sent to the telemetry manager, whether through polling or an event.
    /// Lists must contain only one specific type of data container
    /// </summary>
    private Dictionary<string, List<DataContainer>> collectedData = new Dictionary<string, List<DataContainer>>();

    /// <summary>
    /// Functions called when polling
    /// </summary>
    private List<PollingFunction> pollers = new List<PollingFunction>();
    /// <summary>
    /// Coroutines that call the polling functions
    /// </summary>
    private List<Coroutine> pollingRefs = new List<Coroutine>();

    #region Manager Class
    void Awake()
    {
        if (instance != null)
        {
            //Delete the duplicate
            Destroy(instance.gameObject);
        }

        instance = this;

        //Should we not destroy on load? 

    }


    #endregion

    /// <summary>
    /// Add a polling function for the telemetry manager to call on a regular interval
    /// </summary>
    /// <param name="poller">The function to poll</param>
    /// <param name="pollingRateInSeconds">How often to call the function, in seconds</param>
    /// <param name="dataKey">Optionally, a specific dataKey to store this data under. Uses the container's class name if not provided</param>
    public void AddPoller(PollingFunction poller, float pollingRateInSeconds = 1f, string dataKey = null)
    {
        pollers.Add(poller);
        pollingRefs.Add(StartCoroutine(Poll(poller, pollingRateInSeconds, dataKey)));
    }

    /// <summary>
    /// Remove a previously added polling function
    /// </summary>
    /// <param name="poller">The function to poll</param>
    public void RemovePoller(PollingFunction poller)
    {
        int index = pollers.IndexOf(poller);
        if (index != -1)
        {
            StopCoroutine(pollingRefs[index]);
            pollers.RemoveAt(index);
            pollingRefs.RemoveAt(index);
        }
    }

    /// <summary>
    /// Continuously poll the given function and store its data
    /// </summary>
    /// <param name="poller">The function to poll</param>
    /// <param name="pollingRateInSeconds">How often to poll the given function, per second</param>
    /// <param name="dataKey">Optionally, a specific dataKey to store this data under. Uses the container's class name if not provided</param>
    /// <returns></returns>
    private IEnumerator Poll(PollingFunction poller, float pollingRateInSeconds, string dataKey = null)
    {
        while (true)
        {
            DataContainer data = poller();
            if (data != null)
            {
                RecordData(data, dataKey);
            }
            yield return new WaitForSeconds(pollingRateInSeconds);
        }
    }

    /// <summary>
    /// Record a single datum
    /// </summary>
    /// <param name="data">The data to store</param>
    /// <param name="dataKey">Optionally, a specific dataKey to store this data under. Uses the container's class name if not provided</param>
    public void RecordData(DataContainer data, string dataKey = null)
    {
        if (dataKey == null)
        {
            dataKey = data.GetType().Name;
        }
        if (!this.collectedData.ContainsKey(dataKey))
        {
            this.collectedData.Add(dataKey, new List<DataContainer>() { data });
        }
        else
        {
            this.collectedData[dataKey].Add(data);
        }
    }

    /// <summary>
    /// Exports all the collected data to their respective csv files
    /// </summary>
    [EasyButtons.Button]
    public void ExportData()
    {
        if (CsvManager.instance == null)
        {
            Debug.LogError("CsvManager not found. Please add CsvManager to the scene.");
            return;
        }

        foreach (KeyValuePair<string, List<DataContainer>> entry in collectedData)
        {
            CsvManager.instance.makeCsvWithMultipleEntrys(entry.Value, $"{entry.Key}.csv");
        }
    }
}
