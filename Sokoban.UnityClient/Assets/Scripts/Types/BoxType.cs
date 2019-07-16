using UnityEngine;

public enum BoxType
{
    [Sprite(Key = Marker.DefaultSpriteKey, Name = "marker_blue")]
    [Sprite(Key = Box.DefaultSpriteKey, Name = "box_blue")]
    [Thumbnail(Key = Box.DefaultSpriteKey, Name = "thumbnail_box_blue")]
    [Thumbnail(Key = Marker.DefaultSpriteKey, Name = "thumbnail_marker_blue")]
    [Resource(Type = ResourceAttribute.ResourceType.Material, Path = "Materials/MarkerBlue")]
    Blue,

    [Sprite(Key = Marker.DefaultSpriteKey, Name = "marker_brown")]
    [Sprite(Key = Box.DefaultSpriteKey, Name = "box_brown")]
    [Thumbnail(Key = Box.DefaultSpriteKey, Name = "thumbnail_box_brown")]
    [Thumbnail(Key = Marker.DefaultSpriteKey, Name = "thumbnail_marker_brown")]
    [Resource(Type = ResourceAttribute.ResourceType.Material, Path = "Materials/MarkerBrown")]
    Brown,

    [Sprite(Key = Marker.DefaultSpriteKey, Name = "marker_gray")]
    [Sprite(Key = Box.DefaultSpriteKey, Name = "box_gray")]
    [Thumbnail(Key = Box.DefaultSpriteKey, Name = "thumbnail_box_gray")]
    [Thumbnail(Key = Marker.DefaultSpriteKey, Name = "thumbnail_marker_gray")]
    [Resource(Type = ResourceAttribute.ResourceType.Material, Path = "Materials/MarkerGray")]
    Gray,

    [Sprite(Key = Marker.DefaultSpriteKey, Name = "marker_green")]
    [Sprite(Key = Box.DefaultSpriteKey, Name = "box_green")]
    [Thumbnail(Key = Box.DefaultSpriteKey, Name = "thumbnail_box_green")]
    [Thumbnail(Key = Marker.DefaultSpriteKey, Name = "thumbnail_marker_green")]
    [Resource(Type = ResourceAttribute.ResourceType.Material, Path = "Materials/MarkerGreen")]
    Green,

    [Sprite(Key = Marker.DefaultSpriteKey, Name = "marker_red")]
    [Sprite(Key = Box.DefaultSpriteKey, Name = "box_red")]
    [Thumbnail(Key = Box.DefaultSpriteKey, Name = "thumbnail_box_red")]
    [Thumbnail(Key = Marker.DefaultSpriteKey, Name = "thumbnail_marker_red")]
    [Resource(Type = ResourceAttribute.ResourceType.Material, Path = "Materials/MarkerRed")]
    Red
}
