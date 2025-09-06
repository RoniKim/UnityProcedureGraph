# UnityProcedureGraph

Node based procedure/flow graph editor & runtime runner for Unity.

[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Last Commit](https://img.shields.io/github/last-commit/RoniKim/UnityProcedureGraph)](https://github.com/RoniKim/UnityProcedureGraph/commits/main)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/RoniKim/UnityProcedureGraph/pulls)
[![Open Issues](https://img.shields.io/github/issues/RoniKim/UnityProcedureGraph)](https://github.com/RoniKim/UnityProcedureGraph/issues)

---

## 0) TL;DR
UnityProcedureGraph는 [Node Graph Processor](https://github.com/alelievr/NodeGraphProcessor)를 기반으로 한 절차 그래프 제작 도구이자 런타임 실행 시스템입니다. 그래프를 JSON으로 저장하고 `RuntimeProcessorRunner`로 재생할 수 있습니다.

핵심 UI 미리보기:

![Overview](docs/images/overview.png)
---

## 1) Features / 핵심 기능
- 노드 기반 절차/조건 그래프 생성
- `Wintek/ProcessCreatorWindow` 메뉴로 에디터 호출
- 툴바에서 **Load Asset**, **Graph SaveToJson**, **Graph LoadToJson** 버튼 제공
- `RuntimeProcessorRunner`를 통한 그래프 런타임 실행 및 노드 변경 이벤트
- `GraphSerializable`로 그래프 ↔ JSON 직렬화
- `StartNode`, `MultipleConditionNode`, `StringListNode` 등 샘플 노드 포함
- [UniTask](https://github.com/Cysharp/UniTask)를 활용한 비동기 처리
- 커스텀 노드/뷰 확장 및 API 제공
---

## 2) Demo / Screenshot


`docs/images/overview.png`에 스크린샷을 추가하세요 (1920×1080, 다크 테마 권장). 중요한 UI 요소가 보이도록 캡처합니다.

---

## 3) Installation / 설치

1. **Unity 버전**: 2022.3.2f1 이상 권장.
2. **Git 클론**: 이 리포지토리를 그대로 프로젝트로 사용하거나 `Assets/000.Script`와 `Assets/Editor` 폴더를 복사합니다.
3. **UPM (실험적)**: `Window > Package Manager > Add package from git URL...`에서 아래 주소를 입력합니다. *(패키지 정의가 없어 실패할 수 있습니다)*
   ```
   https://github.com/RoniKim/UnityProcedureGraph.git?path=Assets/000.Script
   ```
   추가로 `Assets/Editor`를 프로젝트에 포함해야 합니다.
4. **의존성**
   - Node Graph Processor 1.3.0
   - UniTask 2.5.10
