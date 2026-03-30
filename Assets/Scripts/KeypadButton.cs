using UnityEngine;
using System.Collections;

public class KeypadButton : MonoBehaviour
{
    public string value; 
    [SerializeField] private float bttnspeed = 0.05f;
    [SerializeField] private float moveDist = 0.005f;

    private bool moving;

    // Função pública que o Cérebro (Keypad.cs) vai chamar
    public void Pressionar()
    {
        if (!moving)
        {
            StartCoroutine(MoveSmooth());
        }
    }

    private IEnumerator MoveSmooth()
    {
        moving = true;
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = transform.localPosition + new Vector3(0, 0, moveDist);

        float elapsedTime = 0;
        while (elapsedTime < bttnspeed)
        {
            elapsedTime += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(startPos, endPos, elapsedTime / bttnspeed);
            yield return null;
        }
        yield return new WaitForSeconds(0.05f);
        transform.localPosition = startPos;
        moving = false;
    }
}