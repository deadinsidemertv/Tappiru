using System;
using System.Collections.Generic;
using System.Text;
using TappiruCS.UI.Sprite;

namespace TappiruCS.State.Edit.Core
{
    internal record Marker(float Time, string Text, SpriteObject Visual);
}
