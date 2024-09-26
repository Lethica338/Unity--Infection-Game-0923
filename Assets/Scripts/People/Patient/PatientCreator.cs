﻿using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class PatientCreator
{
    public int numberOfInpatient;
    public int numberOfOutpatient;
    public int numberOfEmergencyPatient;
    public List<Waypoint> spawnAreas = new List<Waypoint>();

    public float infectionRate = 0.03f; // 감염 확률
    public float spawnDelay = 1f; // 생성 대기 시간

    public bool outpatientWaiting;
    public bool emergencyPatientWaiting;
    public bool startSignal = false;

    ProfileWindow profileWindow;

    // Start is called before the first frame update
    public void Init()
    {
        profileWindow = Object.FindObjectOfType<ProfileWindow>();

        Transform gatewayTransform = Managers.NPCManager.gatewayTransform; // "Gateways" 오브젝트 찾기



        if (gatewayTransform != null)
        {

            for (int i = 0; i < gatewayTransform.childCount; i++)
            {
                Waypoint waypointRange = gatewayTransform.GetChild(i).GetComponent<Waypoint>();
                if (waypointRange != null)
                {
                    spawnAreas.Add(waypointRange); // Waypoint 리스트에 추가
                }
                else
                {
                    Debug.LogError("Gateways 자식 오브젝트에 Waypoint 컴포넌트가 없습니다.");
                }
            }
        }
        else
        {
            Debug.LogError("Gateways 게임 오브젝트를 찾을 수 없습니다.");
        }

        for (int i = 0; i < Managers.ObjectPooling.maxOfInpatient * 0.7; i++)
        {
            // 입원 환자 스폰 위치 설정
            int ward = (i / 40) + 4;
            BedWaypoint spawnArea = Managers.NPCManager.waypointDictionary[(ward, "InpatientWaypoints")].Find("BedWaypoint (" + (i % 40) + ")").gameObject.GetComponent<BedWaypoint>();
            GameObject newInpatient = Managers.ObjectPooling.ActivateInpatient(spawnArea.GetMiddlePointInRange());
            newInpatient.name = "Inpatient " + i;

            // 입원 환자 위치별 Layer 설정


            if (ward >= 0 && ward < LayerChangeManager.Instance.layers.Length)
            {
                LayerChangeManager.SetLayerRecursively(newInpatient, LayerMask.NameToLayer(LayerChangeManager.Instance.layers[ward]));
            }

            spawnArea.patient = newInpatient;
            PatientController newInpatientController = newInpatient.GetComponent<PatientController>();
            newInpatientController.waypointsTransform = Managers.NPCManager.waypointDictionary[(ward, "InpatientWaypoints")];
            newInpatientController.ward = ward;
            newInpatientController.num = i;
            newInpatientController.bedWaypoint = spawnArea;
            Transform waypointTransform = newInpatientController.bedWaypoint.transform.parent;
            newInpatientController.wardComponent = waypointTransform.parent.GetComponent<Ward>();
            //newInpatientController.wardComponent.totalOfNPC++;
            newInpatientController.wardComponent.inpatients.Add(newInpatientController);


            profileWindow.AddInpatientProfile(newInpatient);
            numberOfInpatient++;
        }
    }
    public IEnumerator SpawnOutpatient()
    {
        outpatientWaiting = true; // 대기 상태로 설정
        yield return new WaitUntil(() => startSignal);
        bool isClosed = false;
        for (int i = 0; i < 6; i++)
        {
            if (!Managers.NPCManager.waypointDictionary[(i, "OutpatientWaypoints")].gameObject.GetComponentInParent<Ward>().isClosed)
            {
                break;
            }
            else
            {
                isClosed = true;
            }
        }
        if (isClosed)
        {
            yield break;
        }
        Vector3 spawnPosition = spawnAreas[Random.Range(0, 2)].GetRandomPointInRange(); // 랜덤 생성 위치 설정
        GameObject newOutpatient = Managers.ObjectPooling.ActivateOutpatient(spawnPosition); // 외래 환자 활성화
        newOutpatient.GetComponent<Person>().role = Role.Outpatient;
        if (newOutpatient != null)
        {
            Person newOutPatientPerson = newOutpatient.GetComponent<Person>(); // Person 컴포넌트 가져오기
            if (newOutPatientPerson != null)
            {
                // 감염 상태 설정
                if (Random.value < infectionRate)
                {
                    if (Managers.Stage.stage == 1)
                    {
                        newOutPatientPerson.ChangeStatus(InfectionState.CRE);
                    }
                    else if (Managers.Stage.stage == 2)
                    {
                        newOutPatientPerson.ChangeStatus(InfectionState.Covid);
                    }
                }
                numberOfOutpatient++; // 외래 환자 수 증가
            }
            else
            {
                Debug.LogError("새 외래 환자에 Person 컴포넌트가 없습니다.");
            }
        }
        else
        {
            Debug.LogError("새 외래 환자를 활성화하는 데 실패했습니다.");
        }

        yield return new WaitForSeconds(spawnDelay); // 대기 시간
        outpatientWaiting = false; // 대기 상태 해제
    }
    public IEnumerator SpawnEmergencyPatient()
    {
        emergencyPatientWaiting = true;
        yield return new WaitUntil(() => startSignal);

        Vector3 spawnPosition = spawnAreas[2].GetRandomPointInRange(); // 랜덤 생성 위치 설정
        GameObject newEmaergencyPatient = Managers.ObjectPooling.ActiveEmergentcyPatient(spawnPosition); // 응급 환자 활성화
        newEmaergencyPatient.GetComponent<Person>().role = Role.EmergencyPatient;
        if (newEmaergencyPatient != null)
        {
            Person newEmergencyPatientPerson = newEmaergencyPatient.GetComponent<Person>(); // Person 컴포넌트 가져오기
            if (newEmergencyPatientPerson != null)
            {
                // 감염 상태 설정
                if (Random.value < infectionRate)
                {
                    if (Managers.Stage.stage == 1)
                    {
                        newEmergencyPatientPerson.ChangeStatus(InfectionState.CRE);
                    }
                    else if (Managers.Stage.stage == 2)
                    {
                        newEmergencyPatientPerson.ChangeStatus(InfectionState.Covid);
                    }
                }
                numberOfEmergencyPatient++; // 외래 환자 수 증가
            }
            else
            {
                Debug.LogError("새 응급 환자에 Person 컴포넌트가 없습니다.");
            }
        }
        else
        {
            Debug.LogError("새 응급 환자를 활성화하는 데 실패했습니다.");
        }

        yield return new WaitForSeconds(spawnDelay); // 대기 시간
        emergencyPatientWaiting = false; // 대기 상태 해제

    }
}
