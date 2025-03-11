using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("UI Element References")]
    
    [SerializeField] private Image potentialEnergyFill;
    
    [SerializeField] private Image kineticEnergyFill;
    
        //Dynamic, Non - Serialized Below
    
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    #region Setters

    public void SetPotentialEnergyFill(float fillAmount)
    {
        if (potentialEnergyFill != null) potentialEnergyFill.fillAmount = fillAmount;
    }
    
    public void SetKineticEnergyFill(float fillAmount)
    {
        if (kineticEnergyFill != null) kineticEnergyFill.fillAmount = fillAmount;
    }

    #endregion
}
