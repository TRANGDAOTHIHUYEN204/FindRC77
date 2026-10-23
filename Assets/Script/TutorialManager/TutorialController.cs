using System.Collections;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private HintSpotlight spotlight;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private GameObject[] hideDuringTutorial;
    [SerializeField] private int expectedTutorialPoints = 5;
    [SerializeField] private float finishDelay = 1f;

    private DifferentPair _target;

}