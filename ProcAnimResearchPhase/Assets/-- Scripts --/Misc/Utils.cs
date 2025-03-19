using UnityEngine;

public enum EScreenPos
    {
        TopLeft,
        TopCenter,
        TopRight,
        RightCenter,
        BottomRight,
        BottomCenter,
        BottomLeft,
        LeftCenter,
        Center
    }
public class Utils : MonoBehaviour
{
    // This method is used to determine the placement of a button based on given bounds and a button rect
    public static Vector2 DeterminePlacement(Vector2 min, Vector2 max, Rect buttonRect, EScreenPos placement)
    {
        Vector2 targetPos = Vector2.zero;
        
        switch (placement)
        {
            case EScreenPos.TopLeft:
                targetPos = new Vector2(min.x + buttonRect.width / 2, max.y - buttonRect.height / 2);
                break;
            case EScreenPos.TopCenter:
                targetPos = new Vector2((min.x + max.x) / 2, max.y - buttonRect.height / 2);
                break;
            case EScreenPos.TopRight:
                targetPos = new Vector2(max.x - buttonRect.width / 2, max.y - buttonRect.height / 2);
                break;
            case EScreenPos.RightCenter:
                targetPos = new Vector2(max.x - buttonRect.width / 2, (min.y + max.y) / 2);
                break;
            case EScreenPos.BottomRight:
                targetPos = new Vector2(max.x - buttonRect.width / 2, min.y + buttonRect.height / 2);
                break;
            case EScreenPos.BottomCenter:
                targetPos = new Vector2((min.x + max.x) / 2, min.y + buttonRect.height / 2);
                break;
            case EScreenPos.BottomLeft:
                targetPos = new Vector2(min.x + buttonRect.width / 2, min.y + buttonRect.height / 2);
                break;
            case EScreenPos.LeftCenter:
                targetPos = new Vector2(min.x + buttonRect.width / 2, (min.y + max.y) / 2);
                break;
            case EScreenPos.Center:
                targetPos = new Vector2((min.x + max.x) / 2, (min.y + max.y) / 2);
                break;
        }
        
        return targetPos;
    }
    public static bool IsLayerInLayerMask(int layer, LayerMask layerMask)
    {
        return ((1 << layer) & layerMask.value) != 0;
    }

}
