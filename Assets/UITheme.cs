using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 코드로 생성하는 UI 전반에 일관된 룩앤필을 적용하기 위한 공용 테마 헬퍼.
// 솔로 레벨링(그림자 군주) 톤: 딥 인디고 배경 + 바이올렛/시안 네온 + 골드 강조.
public static class UITheme
{
    // ---- 팔레트 ----
    public static readonly Color Bg         = new Color32(0x0B, 0x0B, 0x16, 255); // 거의 검은 네이비
    public static readonly Color BarBg      = new Color32(0x12, 0x10, 0x20, 250); // 하단바/상단바
    public static readonly Color PanelBg    = new Color32(0x18, 0x15, 0x2B, 245); // 팝업 패널
    public static readonly Color PanelSoft  = new Color32(0x24, 0x20, 0x3E, 235); // 보조 패널/비활성 버튼
    public static readonly Color Primary    = new Color32(0x63, 0x45, 0xE8, 255); // 바이올렛 (주요 버튼/활성 탭)
    public static readonly Color Accent     = new Color32(0x2C, 0xD8, 0xF0, 255); // 시안 네온
    public static readonly Color Danger     = new Color32(0xD0, 0x3A, 0x55, 255); // 위험/닫기 계열
    public static readonly Color Gold       = new Color32(0xFF, 0xC9, 0x3C, 255); // 강조(보상/타이틀)
    public static readonly Color HpFill     = new Color32(0xE0, 0x2B, 0x4A, 255); // 보스 HP
    public static readonly Color HpTrack    = new Color32(0x30, 0x12, 0x1C, 235);
    public static readonly Color TextMain   = new Color32(0xEC, 0xEC, 0xF6, 255);
    public static readonly Color TextDim    = new Color32(0x9A, 0x9A, 0xB8, 255);

    private static Sprite _rounded;

    // 유니티 빌트인 9-슬라이스 UI 스프라이트 (둥근 모서리)
    public static Sprite Rounded()
    {
        if (_rounded == null)
            _rounded = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        return _rounded;
    }

    // 이미지를 둥근 패널로 스타일링
    public static void Panel(Image img, Color color)
    {
        if (img == null) return;
        Sprite s = Rounded();
        if (s != null)
        {
            img.sprite = s;
            img.type = Image.Type.Sliced;
        }
        img.color = color;
    }

    // 버튼: 둥근 배경 + 눌림 피드백
    public static void StyleButton(Button btn, Color color)
    {
        if (btn == null) return;
        Panel(btn.GetComponent<Image>(), color);

        btn.transition = Selectable.Transition.ColorTint;
        var cb = btn.colors;
        cb.normalColor = Color.white;                       // 이미지 색과 곱연산 -> 원색 유지
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        cb.pressedColor = new Color(0.78f, 0.78f, 0.85f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.5f, 0.5f, 0.55f, 0.6f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
    }

    // 타이틀 텍스트에 은은한 세로 그라디언트(골드->화이트) - 오브젝트 단위라 안전
    public static void TitleGradient(TextMeshProUGUI t)
    {
        if (t == null) return;
        t.enableVertexGradient = true;
        t.colorGradient = new VertexGradient(Gold, Gold, new Color32(0xFF, 0xF2, 0xC8, 255), new Color32(0xFF, 0xF2, 0xC8, 255));
    }
}
