using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{

    public float timer, refresh, avgFramerate;
    string display = "{0} FPS";
    private TextMeshProUGUI m_Text;

    [SerializeField]
    GameController g;

    float waitTime = 3f;

    private void Start()
    {
        m_Text = GetComponent<TextMeshProUGUI>();
    }


    private void Update()
    {
        if (waitTime > 0)
        {
            avgFramerate = 60;
            waitTime -= Time.deltaTime;
            return;
        }
        //Change smoothDeltaTime to deltaTime or fixedDeltaTime to see the difference
        float timelapse = Time.smoothDeltaTime;
        timer = timer <= 0 ? refresh : timer -= timelapse;

        if (timer <= 0) avgFramerate = (int)(1f / timelapse);
        m_Text.text = string.Format(display, avgFramerate.ToString()) + $"\n{g.Cells.Count}";
        if (avgFramerate < 60)
        {
        //    UnityEditor.EditorApplication.isPaused = true;
        }



    }
}