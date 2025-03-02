using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageFill : MonoBehaviour
{
    //Dynamic, Non - Serialized Below
    
    private Image image;
    
    private void Awake()
    {
        if (image == null) image = GetComponent<Image>();
    }
    
    public float FillAmount { set { if (image != null ) image.fillAmount = value; } } // Sets fill if has
}
