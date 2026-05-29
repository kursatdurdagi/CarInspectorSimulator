using System.Collections.Generic;
using UnityEngine;

public class BodyPart : MonoBehaviour
{
    [Header("Probabilities (0 - 100)")]
    [Range(0, 100)] public float changeChance = 20f;
    [Range(0, 100)] public float paintChance = 20f;
    [Range(0, 100)] public float damageChance = 15f;
    [Range(0, 100)] public float scratchChance = 25f;

    [Header("Body Situation")]
    public bool isOriginal;
    public bool isChanged;
    public bool isPainted;

    [Header("Body Health")]
    public bool isDamaged;
    public bool isScratch;

    [Header("Micron Value")]
    public int micronValue; // Oyuncunun cihazla ölçünce göreceði deðer

    [Header("Visual Lists")]
    [SerializeField] private List<GameObject> damagedVisuals;
    [SerializeField] private List<GameObject> scratchVisuals;

    void Start()
    {
        CalculateStatus();
    }

    private void CalculateStatus()
    {
        float roll = Random.Range(0f, 100f);

        // Tek yüzdeye baðlý durum hesabý
        if (roll <= changeChance)
            isChanged = true;
        else if (roll <= changeChance + paintChance)
            isPainted = true;
        else
            isOriginal = true;

        // Mikron Deðeri Hesaplama
        CalculateMicron();

        // Hasarlý veya Çizik zarlarý
        if (Random.Range(0f, 100f) <= damageChance) SetupVisual(damagedVisuals, ref isDamaged);
        if (Random.Range(0f, 100f) <= scratchChance) SetupVisual(scratchVisuals, ref isScratch);
    }

    private void CalculateMicron()
    {
        if (isPainted)
        {
            // Parça boyalýysa yüksek mikron (Macunlu/Boyalý)
            micronValue = Random.Range(300, 701);
        }
        else
        {
            // Orijinal veya Deðiþen parça fabrikasyon boyalýdýr, mikronu düþük olur
            micronValue = Random.Range(80, 141); // Sektör standardý genelde 80-140 arasýdýr
        }
    }

    private void SetupVisual(List<GameObject> list, ref bool stateBool)
    {
        if (list == null || list.Count == 0) return;

        stateBool = true;
        list.ForEach(obj => obj.SetActive(false));
        list[Random.Range(0, list.Count)].SetActive(true);
    }
}