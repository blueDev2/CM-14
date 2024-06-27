using System.Numerics;
using Content.Client.Cooldown;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Content.Shared._CM14.IconLabel;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Themes;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Controls
{
    [Virtual]
    public abstract class SlotControl : Control, IEntityControl
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        public static int DefaultButtonSize = 64;

        public TextureRect ButtonRect { get; }
        public TextureRect BlockedRect { get; }
        public TextureRect HighlightRect { get; }
        public SpriteView HoverSpriteView { get; }
        public TextureButton StorageButton { get; }
        public CooldownGraphic CooldownDisplay { get; }
        public Label IconLabel { get; }

        //private readonly Font _font;


        private SpriteView SpriteView { get; }

        public EntityUid? Entity => SpriteView.Entity;

        private bool _slotNameSet;

        private string _slotName = "";
        public string SlotName
        {
            get => _slotName;
            set
            {
                //this auto registers the button with it's parent container when it's set
                if (_slotNameSet)
                {
                    Logger.Warning("Tried to set slotName after init for:" + Name);
                    return;
                }
                _slotNameSet = true;
                if (Parent is IItemslotUIContainer container)
                {
                    container.TryRegisterButton(this, value);
                }
                Name = "SlotButton_" + value;
                _slotName = value;
            }
        }

        public bool Highlight { get => HighlightRect.Visible; set => HighlightRect.Visible = value;}

        public bool Blocked { get => BlockedRect.Visible; set => BlockedRect.Visible = value;}

        private string? _blockedTexturePath;
        public string? BlockedTexturePath
        {
            get => _blockedTexturePath;
            set
            {
                _blockedTexturePath = value;
                BlockedRect.Texture = Theme.ResolveTextureOrNull(_blockedTexturePath)?.Texture;
            }
        }

        private string? _buttonTexturePath;
        public string? ButtonTexturePath
        {
            get => _buttonTexturePath;
            set
            {
                _buttonTexturePath = value;
                UpdateButtonTexture();
            }
        }

        private string? _fullButtonTexturePath;
        public string? FullButtonTexturePath
        {
            get => _fullButtonTexturePath;
            set
            {
                _fullButtonTexturePath = value;
                UpdateButtonTexture();
            }
        }


        private string? _storageTexturePath;
        public string? StorageTexturePath
        {
            get => _buttonTexturePath;
            set
            {
                _storageTexturePath = value;
                StorageButton.TextureNormal = Theme.ResolveTextureOrNull(_storageTexturePath)?.Texture;
            }
        }

        private string? _highlightTexturePath;
        public string? HighlightTexturePath
        {
            get => _highlightTexturePath;
            set
            {
                _highlightTexturePath = value;
                HighlightRect.Texture = Theme.ResolveTextureOrNull(_highlightTexturePath)?.Texture;
            }
        }

        public event Action<GUIBoundKeyEventArgs, SlotControl>? Pressed;
        public event Action<GUIBoundKeyEventArgs, SlotControl>? Unpressed;
        public event Action<GUIBoundKeyEventArgs, SlotControl>? StoragePressed;
        public event Action<GUIMouseHoverEventArgs, SlotControl>? Hover;

        public bool EntityHover => HoverSpriteView.Sprite != null;
        public bool MouseIsHovering;

        public SlotControl()
        {
            IoCManager.InjectDependencies(this);

            //var cache = IoCManager.Resolve<IResourceCache>();
            //_font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 8);

            Name = "SlotButton_null";
            MinSize = new Vector2(DefaultButtonSize, DefaultButtonSize);

            AddChild(ButtonRect = new TextureRect
            {
                TextureScale = new Vector2(2, 2),
                MouseFilter = MouseFilterMode.Stop,
            });
            AddChild(HighlightRect = new TextureRect
            {
                Visible = false,
                TextureScale = new Vector2(2, 2),
                MouseFilter = MouseFilterMode.Ignore
            });
            AddChild(IconLabel = new Label
            {
                Text = "TI",
                SetSize = new Vector2(1f, 1f),
                HorizontalAlignment = HAlignment.Right,
                VerticalAlignment = VAlignment.Center,
                Visible = true,
            });

            ButtonRect.OnKeyBindDown += OnButtonPressed;
            ButtonRect.OnKeyBindUp += OnButtonUnpressed;

            AddChild(SpriteView = new SpriteView
            {
                Scale = new Vector2(2, 2),
                SetSize = new Vector2(DefaultButtonSize, DefaultButtonSize),
                OverrideDirection = Direction.South
            });

            AddChild(HoverSpriteView = new SpriteView
            {
                Scale = new Vector2(2, 2),
                SetSize = new Vector2(DefaultButtonSize, DefaultButtonSize),
                OverrideDirection = Direction.South
            });

            AddChild(StorageButton = new TextureButton
            {
                Scale = new Vector2(0.75f, 0.75f),
                HorizontalAlignment = HAlignment.Right,
                VerticalAlignment = VAlignment.Bottom,
                Visible = false,
            });


            /**
            if (_entities.TryGetComponent(Entity, out IconLabelComponent? iconLabel))
            {
                if (Loc.TryGetString(iconLabel.LabelTextLocId, out String? labelText))
                {
                    IconLabel.Text = labelText;
                }

                if (Color.TryFromName(iconLabel.TextColor, out Robust.Shared.Maths.Color color))
                {
                    IconLabel.FontColorOverride = color;
                }

                IconLabel.SetSize = new Vector2(iconLabel.TextSize);
            }
            **/

            StorageButton.OnKeyBindDown += args =>
            {
                if (args.Function != EngineKeyFunctions.UIClick)
                {
                    OnButtonPressed(args);
                }
            };

            StorageButton.OnPressed += OnStorageButtonPressed;

            ButtonRect.OnMouseEntered += _ =>
            {
                MouseIsHovering = true;
            };
            ButtonRect.OnMouseEntered += OnButtonHover;

            ButtonRect.OnMouseExited += _ =>
            {
                MouseIsHovering = false;
                ClearHover();
            };

            AddChild(CooldownDisplay = new CooldownGraphic
            {
                Visible = false,
            });

            AddChild(BlockedRect = new TextureRect
            {
                TextureScale = new Vector2(2, 2),
                MouseFilter = MouseFilterMode.Stop,
                Visible = false
            });

            HighlightTexturePath = "slot_highlight";
            BlockedTexturePath = "blocked";
        }

        public void ClearHover()
        {
            if (!EntityHover)
                return;

            var tempQualifier = HoverSpriteView.Entity;
            if (tempQualifier != null)
            {
                IoCManager.Resolve<IEntityManager>().QueueDeleteEntity(tempQualifier);
            }

            HoverSpriteView.SetEntity(null);
        }

        public void SetEntity(EntityUid? ent)
        {
            SpriteView.SetEntity(ent);
            UpdateButtonTexture();
        }

        private void UpdateButtonTexture()
        {
            var fullTexture = Theme.ResolveTextureOrNull(_fullButtonTexturePath);
            var texture = Entity.HasValue && fullTexture != null
                ? fullTexture.Texture
                : Theme.ResolveTextureOrNull(_buttonTexturePath)?.Texture;
            ButtonRect.Texture = texture;
        }

        private void OnButtonPressed(GUIBoundKeyEventArgs args)
        {
            Pressed?.Invoke(args, this);
        }

        private void OnButtonUnpressed(GUIBoundKeyEventArgs args)
        {
            Unpressed?.Invoke(args, this);
        }

        private void OnStorageButtonPressed(BaseButton.ButtonEventArgs args)
        {
            if (args.Event.Function == EngineKeyFunctions.UIClick)
            {
                StoragePressed?.Invoke(args.Event, this);
            }
            else
            {
                Pressed?.Invoke(args.Event, this);
            }
        }

        private void OnButtonHover(GUIMouseHoverEventArgs args)
        {
            Hover?.Invoke(args, this);
        }

        protected override void OnThemeUpdated()
        {
            base.OnThemeUpdated();

            StorageButton.TextureNormal = Theme.ResolveTextureOrNull(_storageTexturePath)?.Texture;
            HighlightRect.Texture = Theme.ResolveTextureOrNull(_highlightTexturePath)?.Texture;
            UpdateButtonTexture();
        }

        /**protected override void Draw(DrawingHandleScreen handle)
        {
            if (_entities.TryGetComponent(Entity, out IconLabelComponent? iconLabel))
            {
                if (!Loc.TryGetString(iconLabel.LabelTextLocId, out string? msg))
                {
                    return;
                }

                var textColor = Color.Black;
                Color.TryFromName(iconLabel.TextColor, out textColor);


                var charArray = msg.ToCharArray();
                var charPosition = Position;
                charPosition.X += iconLabel.Xoffset;
                charPosition.Y += iconLabel.Yoffset;

                var textSize = iconLabel.TextSize;

                float sep = 0;
                foreach (var chr in charArray)
                {
                    charPosition.X += sep;
                    sep = _font.DrawChar(handle, new System.Text.Rune(chr), charPosition, textSize, textColor);
                }
            }
            base.Draw(handle);
        }**/

        EntityUid? IEntityControl.UiEntity => Entity;
    }
}
