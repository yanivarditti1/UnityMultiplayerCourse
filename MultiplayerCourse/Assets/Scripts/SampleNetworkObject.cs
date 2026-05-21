using UnityEngine;
using Fusion;
using TMPro;

public class SampleNetworkObject : NetworkBehaviour
{
    [Networked]
    public int Counter { get; set;}

    [Networked, OnChangedRender(nameof(ChangeValue))]
    public int Value { get; set;}

    [SerializeField] private TextMeshProUGUI label;
    
    public override void Spawned()
    {
        Debug.Log($"Spawned {Object.ToString()}");
    }
    
    [ContextMenu("Add 1 to counter")]
    public void AddOne()
    {
        Counter++;
       Debug.Log($"Counter: {Counter}");
       if (Counter % 5 == 0)
       {
           ChangeValue();
       }
    }

    private void ChangeValue()
    {
        Value++;
        label.text = Value.ToString();
    }
}
