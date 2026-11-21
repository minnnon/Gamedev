using UnityEngine;

public class Terminal : MonoBehaviour
{
    [TextArea(3, 6)] public string noteText;        
    public float showTime = 4f;        
    private bool activated = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {    if(!activated)
            {
                if (GameManager.instance != null)
                {
                    GameManager.instance.ShowMessage(noteText, showTime);
                    activated = true;
                }
            } 
        }
    }
 }

