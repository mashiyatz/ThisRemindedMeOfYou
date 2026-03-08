using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollThroughBooks : MonoBehaviour
{
    public Transform bookRecs;
    // private Vector3[] bookPositions;
    private int lastIndex;
    private int index;
    private float fill;
    private float threshold;
    private float timeOfLastWheelChange;
    private float timeOfLastIndexChange;

    private bool isSelectionMode;

    void Start()
    {
        index = 0;
        fill = 0;
        threshold = 5f;
        timeOfLastWheelChange = Time.time;
        timeOfLastIndexChange = Time.time;
        isSelectionMode = false;
/*        bookPositions = new Vector3[bookListGameObject.transform.childCount];
        for (int i = 0; i < bookListGameObject.transform.childCount; i++)
        {
            bookPositions[i] = bookListGameObject.transform.GetChild(i).transform.position;
        }*/
        
    }

    void Update()
    {
        lastIndex = index;

        if (Time.time - timeOfLastWheelChange > 0.5f)
        {
            fill = 0;
        }

        if (Input.mouseScrollDelta.y != 0)
        {
            isSelectionMode = true;
            fill += Input.mouseScrollDelta.y;
            if (fill >= threshold)
            {
                index += 1;
                fill = 0;
                timeOfLastIndexChange = Time.time;
            }
            else if (fill < -threshold)
            {
                index -= 1;
                fill = 0;
                timeOfLastIndexChange = Time.time;
            }
            timeOfLastWheelChange = Time.time;
        }

        if (index > bookRecs.childCount - 1) index = 0;
        else if (index < 0) index = bookRecs.childCount - 1;

        if (lastIndex != index)
        {
            bookRecs.GetChild(lastIndex).gameObject.GetComponent<ClickInteraction>().Dim();
            bookRecs.GetChild(index).gameObject.GetComponent<ClickInteraction>().LightUp();
        }

        if (Input.GetMouseButtonDown(0) && (Time.time - timeOfLastIndexChange < 2.0f) && isSelectionMode)
        {
            bookRecs.GetChild(index).gameObject.GetComponent<ClickInteraction>().PlayBookAnimation();
        } else if (Time.time - timeOfLastIndexChange >= 2.0f && isSelectionMode)
        {
            bookRecs.GetChild(lastIndex).gameObject.GetComponent<ClickInteraction>().Dim();
            isSelectionMode = false;
        }
    }
}
