using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 200f; // 떠오르는 속도
    public float destroyTime = 0.5f; // 1초 뒤 사망

    void Start()
    {
        // 태어나자마자 자신의 죽음을 예약 (Memory Leak 방지)
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // 매 프레임마다 위쪽(Vector3.up)으로 이동
        // UI는 스크린 좌표계를 쓰므로 숫자가 좀 커야 티가 납니다.
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);
    }
    
    // 텍스트 내용을 바꿀 수 있는 메서드 (Setter)
    public void SetText(string text)
    {
        GetComponent<TextMeshProUGUI>().text = text;
    }
	
	// [추가] 크리티컬용 설정 함수
    public void SetCritical(string text)
    {
        TextMeshProUGUI tmp = GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = tmp.fontSize * 2.0f; // 글자 크기 2배
        tmp.color = Color.red;              // 빨간색
        tmp.fontStyle = FontStyles.Bold;    // 굵게
        moveSpeed *= 0.5f;                  // 더 빨리 올라감
    }
}