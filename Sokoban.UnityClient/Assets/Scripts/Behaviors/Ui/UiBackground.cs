using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class UiBackground : BaseBehavior
{
    public Camera cameraTarget;
    public float relativeScale = 1.0f;
    public float rotationAngleDelta;
    public SpriteRenderer gradient;
    public bool shouldRotate;

    private SpriteRenderer backgroundTarget;
    private Vector2Int fillCameraSize;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        this.backgroundTarget = this.GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (this.cameraTarget == null)
        {
            return;
        }

        if (this.cameraTarget.pixelWidth != this.fillCameraSize.x ||
            this.cameraTarget.pixelHeight != this.fillCameraSize.y)
        {
            this.SetBackground(this.cameraTarget.pixelWidth, this.cameraTarget.pixelHeight);
            this.fillCameraSize = new Vector2Int(this.cameraTarget.pixelWidth, this.cameraTarget.pixelHeight);
        }

        if (this.Context.isRunning && this.rotationAngleDelta != 0.0f && this.shouldRotate)
        {
            this.backgroundTarget.transform.Rotate(Vector3.forward, this.rotationAngleDelta, Space.Self);
        }
    }

    private void SetBackground(int width, int height)
    {
        var scaleX = width / (this.backgroundTarget.size.x * GameContext.PixelPerUnit) * this.relativeScale;
        var scaleY = height / (this.backgroundTarget.size.y * GameContext.PixelPerUnit) * this.relativeScale;
        this.backgroundTarget.transform.localScale = this.backgroundTarget.transform.localScale
            .WithX(Mathf.Max(scaleX, scaleY))
            .WithY(Mathf.Max(scaleX, scaleY));
        this.backgroundTarget.transform.position = this.backgroundTarget.transform.position
            .WithX(this.cameraTarget.transform.position.x)
            .WithY(this.cameraTarget.transform.position.y);
        if (this.gradient != null)
        {
            this.gradient.transform.position = this.backgroundTarget.transform.position;
            scaleX = width / (this.gradient.size.x * GameContext.PixelPerUnit) * this.relativeScale;
            scaleY = height / (this.gradient.size.y * GameContext.PixelPerUnit) * this.relativeScale;
            this.gradient.transform.localScale = this.gradient.transform.localScale
                .WithX(Mathf.Max(scaleX, scaleY))
                .WithY(Mathf.Max(scaleX, scaleY));
        }
    }
}
