using System;
using UnityEngine;
using UnityEngine.Serialization;


public class ButtonEventSchedule : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        [FormerlySerializedAs("name")]
        public string id = "Event";
        
        public GameObject target;
        
        [Range(0, 23)] public int startHour = 0;
        [Range(0, 59)] public int startMinute = 0;
        [Range(0, 23)] public int endHour = 0;
        [Range(0, 59)] public int endMinute = 0;
        
        public bool everyDay = true;
        public bool[] days = new bool[7];
        public bool forceOn;
        public bool forceOff;

        public bool IsActiveAt(DateTime now)
        {
            if (forceOff) return false;
            if (forceOn) return true;

            int cur = now.Hour * 60 + now.Minute;
            int start = startHour * 60 + startMinute;
            int end = endHour * 60 + endMinute;

            bool inTime;
            DayOfWeek dayToCheck = now.DayOfWeek;

            if (start == end)
            {
                inTime = true;
            }
            else if (start < end)
            {
                inTime = cur >= start && cur < end;
            }
            else
            {
                inTime = cur >= start || cur < end;
                if (cur < end) dayToCheck = now.AddDays(-1).DayOfWeek;
            }

            if (!inTime) return false;
            return everyDay || (days != null && days.Length == 7 && days[(int)dayToCheck]);
        }
    }

    [SerializeField] private Slot[] slots = new Slot[0];
    [SerializeField] private float checkInterval = 1f;
    [SerializeField] private bool useUtc = false;
    
    public event Action<string, bool> OnSlotChanged;

    private float _timer;

    private DateTime Now => useUtc ? DateTime.UtcNow : DateTime.Now;

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        _timer += Time.unscaledDeltaTime;
        if (_timer < checkInterval) return;
        _timer = 0f;
        Refresh();
    }
    
    public void Refresh()
    {
        if (slots == null) return;
        DateTime now = Now;

        foreach (Slot s in slots)
        {
            if (s == null || s.target == null) continue;

            bool active = s.IsActiveAt(now);
            if (s.target.activeSelf != active)
            {
                s.target.SetActive(active);
                OnSlotChanged?.Invoke(s.id, active);
            }
        }
    }
    
    public bool TryGetSlot(string id, out Slot slot)
    {
        slot = null;
        if (slots == null) return false;

        foreach (Slot s in slots)
        {
            if (s != null && s.id == id)
            {
                slot = s;
                return true;
            }
        }
        return false;
    }
    
    public bool IsSlotActive(string id)
    {
        return TryGetSlot(id, out Slot s) && s.IsActiveAt(Now);
    }
    
    public bool IsSlotActive(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length || slots[index] == null)
            return false;
        return slots[index].IsActiveAt(Now);
    }
    public void SetTime(string id, int startHour, int startMinute, int endHour, int endMinute)
    {
        if (!TryGetSlot(id, out Slot s)) return;

        s.startHour = Mathf.Clamp(startHour, 0, 23);
        s.startMinute = Mathf.Clamp(startMinute, 0, 59);
        s.endHour = Mathf.Clamp(endHour, 0, 23);
        s.endMinute = Mathf.Clamp(endMinute, 0, 59);
        Refresh();
    }
    
    public void SetDays(string id, bool everyDay, params DayOfWeek[] openDays)
    {
        if (!TryGetSlot(id, out Slot s)) return;

        s.everyDay = everyDay;
        s.days = new bool[7];
        if (openDays != null)
        {
            foreach (DayOfWeek d in openDays)
                s.days[(int)d] = true;
        }
        Refresh();
    }
    
    public void SetForce(string id, bool forceOn, bool forceOff)
    {
        if (!TryGetSlot(id, out Slot s)) return;

        s.forceOn = forceOn;
        s.forceOff = forceOff;
        Refresh();
    }

    private void OnValidate()
    {
        if (checkInterval < 0.1f) checkInterval = 0.1f;
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            if (slots[i].days == null || slots[i].days.Length != 7)
                slots[i].days = new bool[7];

            for (int j = 0; j < i; j++)
            {
                if (slots[j] != null && slots[j].id == slots[i].id)
                {
                    break;
                }
            }
        }
    }
}