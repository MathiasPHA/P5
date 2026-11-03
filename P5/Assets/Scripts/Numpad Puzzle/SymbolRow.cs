using UnityEngine;
using UnityEngine.UI;

public class SymbolRow : MonoBehaviour
{

    [SerializeField] Image[] slots = new Image[4];
    
    public void Set(Sprite[] symbols)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            bool has = i <symbols.Length && symbols[i] !=null;
            slots[i].sprite = has? symbols[i] : null;
            slots[i].enabled = has;
        }
    }
}
