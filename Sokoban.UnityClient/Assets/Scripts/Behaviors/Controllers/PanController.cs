using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanController : BaseBehavior
{
    public interface IConstraintProvider
    {
        Bounds GetConstraints();
    }

    public float speed = 10.0f;
    public IConstraintProvider constraintProvider;

    private Vector2? originalMouse;
    private Vector3 originalPosition;

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKey(KeyCode.Mouse0))
        {
            if (originalMouse == null)
            {
                originalMouse = Input.mousePosition;
                originalPosition = this.transform.position;
            }
            else
            {
                var scaleFactor = Camera.main.orthographicSize / 2.25f;
                var diffX = (originalMouse.Value.x - Input.mousePosition.x) / GameContext.PixelPerUnit * scaleFactor;
                var diffY = (originalMouse.Value.y - Input.mousePosition.y) / GameContext.PixelPerUnit * scaleFactor;
                this.transform
                    .WithPositionX(originalPosition.x + diffX)
                    .WithPositionY(originalPosition.y + diffY);

            }
        }
        else if (originalMouse != null)
        {
            originalMouse = null;
        }
        else
        {

            // Get the horizontal and vertical axis.
            // By default they are mapped to the arrow keys.
            // The value is in the range -1 to 1
            float translation = Input.GetAxis("Vertical") * this.speed;
            float rotation = Input.GetAxis("Horizontal") * this.speed;

            // Make it move 10 meters per second instead of 10 meters per frame...
            translation *= Time.deltaTime;
            rotation *= Time.deltaTime;

            // Move translation along the object's z-axis
            this.transform.Translate(rotation, translation, 0);
        }


        if (this.constraintProvider != null && !this.constraintProvider.GetConstraints().Contains(this.transform.position))
        {
            this.transform.position = this.constraintProvider.GetConstraints().ClosestPoint(this.transform.position);
        }
    }
}
