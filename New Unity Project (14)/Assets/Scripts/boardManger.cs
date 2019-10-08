using System.Collections;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;
using UnityEngine;

public class boardManger : MonoBehaviour
{
    public int boardRows, boardColumns; //맵 크기
    public int minRoomSize, maxRoomSize; //방의 최소, 최대 크기
    public GameObject floorTile;
    public GameObject[] horiTile;
    public GameObject[] vertTile;
    public GameObject blookTile;
    public GameObject streetLamp;
    public int lampNum;//가로등 거리
    private GameObject[,] boardPositionsFloor;
    private GameObject[,] thingPositionsFloor;//가로등 등 물건들 배열
    public GameObject[] building;//건물 배열
    public GameObject[] car;
    public GameObject trashcan;
    private int[] buildHash;//랜덤 중복 방지위한 해시
    public class SubDungeon
    {
        public SubDungeon left, right; // 트리의 왼쪽, 오른쪽
        public Rect rect;//현재노드의 공간 크기
        public Rect room = new Rect(-1, -1, 0, 0);//현재 공간에서 방을 만들 객체
        public Rect line;
        public int debugId; // 노드의 순서
        public int isline;//가로면 1 세로면 2, 없으면 0;
        public int split;
        private static int debugCount = 0;//트리 전체 카운트

        public SubDungeon(Rect mrect)
        {
            rect = mrect;//노드 생성시 방의 크기 설정
            debugId = debugCount; //생성 순서 설정
            debugCount++;//다음 노드를 위해 카운트 +1
            isline = 0;
        }

        public bool IAmLeaf() // 자식이 없는 잎 노드인가?
        {
            return left == null && right == null; // 왼쪽, 오른쪽 자식노드가 없으면 잎.
        }

        public bool Split(int minRoomSize, int maxRoomSize)//현재 트리의 자식 노드를 추가하는 함수
        {
            if (!IAmLeaf())//자식이 있는 가지 노드면 이미 나누었으므로 나눌 필요 없음
            {
                return false;
            }

            //가로로 자를지 세로로 자를지 정함
            //만약 너무 넓으면 세로로, 길면 가로로 자름
            //정사각형에 가깝다면 가로, 세로를 랜덤으로 정함

            bool splitH;//true 면 가로, false면 세로

            if (rect.width / rect.height >= 1.25)//밑변이 길면 세로로 자름
            {
                splitH = false;
            }
            else if (rect.height / rect.width >= 1.25)//높이가 길면 가로로 자름
            {
                splitH = true;
            }
            else//거의 같으면 랜덤으로 정함
            {
                splitH = Random.Range(0.0f, 1.0f) > 0.5;
            }

            if (Mathf.Min(rect.height, rect.width) / 2 < minRoomSize)//최소 길이보다 더 짧아지면 나누기 종료
            {
                return false;
            }

            if (splitH)
            {//너무 작지 않게 가로로 자름
                split = Random.Range(minRoomSize, (int)(rect.width - minRoomSize));
                isline = 1;
                left = new SubDungeon(new Rect(rect.x, rect.y, rect.width, split));//현재 좌표에서 랜덤으로 정해진 높이만큼의 공간을 가짐
                right = new SubDungeon(new Rect(rect.x, rect.y + split + 7, rect.width, rect.height - split - 7));//자른 만큼의 y 좌표를 이동하여 자르고 나머지 공간을 가짐
            }
            else
            {//너무 작지 않게 세로로 자름
                split = Random.Range(minRoomSize, (int)(rect.height - minRoomSize));
                isline = 2;
                left = new SubDungeon(new Rect(rect.x, rect.y, split, rect.height));//현재 좌표에서 랜덤으로 정해진 밑변 길이 만큼의 공간을 가짐
                right = new SubDungeon(new Rect(rect.x + split + 7, rect.y, rect.width - split - 7, rect.height));//자르고 남은 공간 차지하는 객체를 생성

            }

            return true;//트리가 나눠졌음을 확인
        }

        public void CreateRoom()//나눠진 공간에 방을 만드는 작업
        {
            if (left != null)
            {
                left.CreateRoom();
            }
            if (right != null)
            {
                right.CreateRoom();
            }
            if (IAmLeaf())//잎 노드는 충분히 나눠진 공간이므로 바로 방을 만듦
            {
                int roomWidth = (int)Random.Range(rect.width / 2, rect.width - 2);
                int roomHeight = (int)Random.Range(rect.height / 2, rect.height - 2);
                int roomX = (int)Random.Range(1, rect.width - roomWidth - 1);
                int roomY = (int)Random.Range(1, rect.height - roomHeight - 1);

                room = new Rect(rect.x + roomX, rect.y + roomY, roomWidth, roomHeight);
            }
        }
        public void CreateLine() //공간을 따라 길의 정보를 입력하는 함수
        {
            if (left != null) //왼쪽 자식 노드가 있다면 왼쪽 자식부터 입력
            {
                left.CreateLine();
            }
            if (right != null) //오른쪽 자식 노드가 있다면 오른쪽 자식부터 입력
            {
                right.CreateLine();
            }

            if (isline == 1) // 가로로 공간을 나누었을시 현재 x좌표와 밑변길이, 나눠져서 옮겨진 y좌표와 가로선을 표시할 높이 1을 넣은 line 객체 생성 
            {
                line = new Rect(rect.x, rect.y + split, rect.width, 7);

            }
            if (isline == 2)// 위와 반대
            {
                line = new Rect(rect.x + split, rect.y, 7, rect.height);
            }
        }
    }

    public void CreateBSP(SubDungeon subDungeon)// 트리 만들기
    {
        if (subDungeon.IAmLeaf())//잎 노드일때
        {//나눈 방이 많이 클 시
            if (subDungeon.rect.width > maxRoomSize ||
                subDungeon.rect.height > maxRoomSize ||
                Random.Range(0.0f, 1.0f) > 0.25)
            {
                if (subDungeon.Split(minRoomSize, maxRoomSize))
                {
                    CreateBSP(subDungeon.left);
                    CreateBSP(subDungeon.right);
                }
            }

        }
    }

    public void DrawRooms(SubDungeon subDungeon) // 주어진 공간에서 작은 방 만드는 함수
    {
        if (subDungeon == null)
        {
            return;
        }
        if (subDungeon.IAmLeaf()) // 잎 노드면 공간이 충분히 나눠 졌으므로 방 생성 시작
        {
            for (int i = (int)subDungeon.rect.x; i < subDungeon.rect.xMax; i++)
            {
                for (int j = (int)subDungeon.rect.y; j < subDungeon.rect.yMax; j++)
                {
                    GameObject instance = Instantiate(floorTile, new Vector3(i, j, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[i, j] = instance;

                }
            }
        }
        else
        {
            DrawRooms(subDungeon.left);
            DrawRooms(subDungeon.right);
        }
    }

    public void DrawLines(SubDungeon subDungeon) //길 까는 함수
    {
        if (subDungeon == null)
        {
            return;
        }
        if (!subDungeon.IAmLeaf()) // 서브 던전 클래스에 있는 라인에 대한 정보를 이용하여 길을 그림. 방을 나누지 않았으면(즉 잎 노드이면) 이 정보가 없음.
        {
            if (subDungeon.isline == 1) //가로로 나눴을 경우
            {
                for (int i = (int)subDungeon.line.x; i < subDungeon.line.xMax; i++) // y값 고정 후 x값을 1씩 올려 길을 만듦
                {

                    GameObject instance = Instantiate(horiTile[0], new Vector3(i, subDungeon.line.y, 1f), Quaternion.Euler(0, 180, 180)) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[i, (int)subDungeon.line.y] = instance;

                    instance = Instantiate(horiTile[1], new Vector3(i, subDungeon.line.y + 1, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[i, (int)subDungeon.line.y + 1] = instance;

                    instance = Instantiate(horiTile[1], new Vector3(i, subDungeon.line.y + 2, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[i, (int)subDungeon.line.y + 2] = instance;

                    instance = Instantiate(horiTile[2], new Vector3(i, subDungeon.line.y + 3, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[i, (int)subDungeon.line.y + 3] = instance;

                    instance = Instantiate(horiTile[1], new Vector3(i, subDungeon.line.y + 4, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[i, (int)subDungeon.line.y + 4] = instance;

                    instance = Instantiate(horiTile[1], new Vector3(i, subDungeon.line.y + 5, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[i, (int)subDungeon.line.y + 5] = instance;

                    instance = Instantiate(horiTile[0], new Vector3(i, subDungeon.line.y + 6, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[i, (int)subDungeon.line.y + 6] = instance;

                }
            }
            else if (subDungeon.isline == 2)//세로로 나눴을 경우
            {
                for (int i = (int)subDungeon.line.y; i < subDungeon.line.yMax; i++)//x값 고정 후 y값을 1씩 올려 길을 만듦
                {
                    GameObject instance = Instantiate(vertTile[0], new Vector3(subDungeon.line.x, i, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[(int)subDungeon.line.x, i] = instance;

                    instance = Instantiate(vertTile[1], new Vector3(subDungeon.line.x + 1, i, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[(int)subDungeon.line.x + 1, i] = instance;

                    instance = Instantiate(vertTile[1], new Vector3(subDungeon.line.x + 2, i, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[(int)subDungeon.line.x + 2, i] = instance;

                    instance = Instantiate(vertTile[2], new Vector3(subDungeon.line.x + 3, i, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[(int)subDungeon.line.x + 3, i] = instance;

                    instance = Instantiate(vertTile[1], new Vector3(subDungeon.line.x + 4, i, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[(int)subDungeon.line.x + 4, i] = instance;

                    instance = Instantiate(vertTile[1], new Vector3(subDungeon.line.x + 5, i, 1f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[(int)subDungeon.line.x + 5, i] = instance;

                    instance = Instantiate(vertTile[0], new Vector3(subDungeon.line.x + 6, i, 1f), Quaternion.Euler(180,0,180)) as GameObject;
                    instance.transform.SetParent(transform);
                    boardPositionsFloor[(int)subDungeon.line.x + 6, i] = instance;


                }
            }
            Vehicle(subDungeon);
        }

        DrawLines(subDungeon.left); // 길을 그린 후 다음 방의 길 연결
        DrawLines(subDungeon.right); // 왼쪽 노드 그린 후 오른쪽 노드 길 연결

    }
    private bool IsNear(GameObject newObect,int num)//현재 오브젝트 주위에 다른 오브젝트가 있는지 확인하는 함수
    {
        bool near=false;//가까이 있으면 true, 주위에 없으면 false
        int x = (int)newObect.transform.position.x < num ? 0 : (int)newObect.transform.position.x-num;//시작할 x좌표값 설정. 10*10의 범위에서 3칸을 기준으로 x 좌표가 3보다 작으면 0부터 시작 
        int y = (int)newObect.transform.position.y < num ? 0 : (int)newObect.transform.position.y- num;//만약 좌표가 기준 거리보다 길 시 현재 좌표에서 기준 거리를 뺀 값을 지정 
        int xMax = (int)newObect.transform.position.x + num > boardRows ? boardRows : (int)newObect.transform.position.x + num;//현재 좌표에서 기준 거리값을 더했을 때 
        int yMax = (int)newObect.transform.position.y + num > boardColumns ? boardColumns : (int)newObect.transform.position.y + num;//도시의 총 배열 크기를 넘어가는 것을 방지
        for (int i = x; i < xMax; i++)
        {
            for (int j = y; j < yMax; j++) {
                if (thingPositionsFloor[i, j] == null) continue;//현재 좌표에 오브젝트가 없으면 넘어감
                if (thingPositionsFloor[i, j].tag == streetLamp.tag)
                {
                    if ((int)(Vector3.Distance(newObect.transform.position, thingPositionsFloor[i, j].transform.position)) < num)//현재 오브젝트와 좌표상의 오브젝트 거리 측정
                    {
                        near = true;// 기준값보다 가까이 있으면 true
                        break;
                    }
                }
            }
        }
        return near;
    }
    public void DrawLamp(SubDungeon subDungeon)//가로등 생성 함수
    {
        if(subDungeon == null)
        {
            return;
        }
        if (!subDungeon.IAmLeaf())
        {
            string rname, buildname,tname;//차도, 건물, 가로등이 같은 구역에 있는지 확인할 변수
            rname = vertTile[0].tag;
            buildname = building[0].tag;
            tname = streetLamp.tag;
            if (subDungeon.isline == 1) //가로로 나눴을 경우
            {
                for (int i = (int)subDungeon.line.x; i < subDungeon.line.xMax; i++) // y값 고정 후 x값을 1씩 올려 길을 만듦
                {
                    if (i % lampNum == 0)// 가로등을 위 아래 교차해서 생성.
                    {
                        string bname = boardPositionsFloor[i, (int)subDungeon.line.y - 1].tag;//현재 좌표
                        if (bname != rname &&bname!=tname && bname!= buildname) // 현제 좌표에 도로, 건물, 같은 가로등이 있는가?
                        {

                            GameObject instance = Instantiate(streetLamp, new Vector3(i, subDungeon.line.y - 1, -2), Quaternion.identity) as GameObject;
                            if (!IsNear(instance, lampNum)) // 현재 생성된 가로등의 주위에 다른 가로등이 있는가?
                            {
                                instance.transform.SetParent(transform);
                                thingPositionsFloor[i, (int)subDungeon.line.y - 1] = instance;
                            }
                            else
                            {
                                Destroy(instance);
                            }
                        }
                    }
                    if (i % lampNum == (int)(lampNum/2))// 가로등을 위 아래 교차해서 생성.
                    {
                        string bname = boardPositionsFloor[i, (int)subDungeon.line.y + 8].tag;
                        if  (bname != rname && bname != tname && bname != buildname)
                        {
                            GameObject instance = Instantiate(streetLamp, new Vector3(i, subDungeon.line.y + 8, -2), Quaternion.identity) as GameObject;
                            if (!IsNear(instance, lampNum))
                            {
                                instance.transform.SetParent(transform);
                                thingPositionsFloor[i, (int)subDungeon.line.y + 8] = instance;
                            }
                            else
                            {
                                Destroy(instance);
                            }
                        }
                    }
                    
                }
            }
            else if (subDungeon.isline == 2)//세로로 나눴을 경우
            {
                for (int i = (int)subDungeon.line.y; i < subDungeon.line.yMax; i++)//x값 고정 후 y값을 1씩 올려 길을 만듦
                {
                    if (i % lampNum == 0)//가로등을 좌우로 배치
                    {
                        string bname = boardPositionsFloor[(int)subDungeon.line.x - 1, i].tag;
                        if (bname != rname && bname != tname && bname != buildname)
                        {
                            GameObject instance = Instantiate(streetLamp, new Vector3(subDungeon.line.x - 1, i, -2), Quaternion.identity) as GameObject;
                            if (!IsNear(instance,  lampNum))
                            {
                                instance.transform.SetParent(transform);
                                thingPositionsFloor[(int)subDungeon.line.x - 1, i] = instance;
                            }
                            else
                            {
                                Destroy(instance);
                            }
                        }

                    }
                    if (i % lampNum == (int)(lampNum / 2))//가로등을 좌우로 배치
                    {
                        string bname = boardPositionsFloor[(int)subDungeon.line.x + 8, i].tag;
                        if (bname != rname && bname != tname && bname != buildname)
                        {
                            GameObject instance = Instantiate(streetLamp, new Vector3(subDungeon.line.x +8 , i, -2), Quaternion.identity) as GameObject;
                            if (!IsNear(instance,  lampNum))
                            {
                                instance.transform.SetParent(transform);
                                thingPositionsFloor[(int)subDungeon.line.x + 8, i] = instance;
                            }
                            else
                            {
                                Destroy(instance);
                            }
                        }

                    }
                }
            }
        }
        DrawLamp(subDungeon.left); 
        DrawLamp(subDungeon.right); 
    }

    public void DrawBuilding(SubDungeon subDungeon)//건물 생성 함수
    {
        hashClear();//건물 배치가 안되는(너무작은)방일 경우 빠져나가기 위한 해시 배열 초기화
        int buildChoice;//랜덤 건물 선택 변수
        Rect buildSize;//현재 건물의 사이즈 표시
        bool isfull = false;//사이즈가 안되는 건물 표시
        int num=0;

        if (subDungeon == null)
        {
            return;
        }
        if (subDungeon.IAmLeaf())
        {

            while (true)//방 사이즈와 건물 사이즈가 맞는지 검사
            {
                num = 0;
                buildChoice = Random.Range(0, building.Length);//건물 번호 랜덤 생성
                if (buildHash[buildChoice] > 0) continue;//이미 선택된 건물일 경우 다시 검사
                buildHash[buildChoice]++;//현재 건물 번호의 해시 배열 증가 (예) 1번 건물 생성시 hash[1] 1증가 
                buildSize = building[buildChoice].GetComponent<RectTransform>().rect;//건물 사이즈 저장
                if (buildSize.width < subDungeon.rect.width && buildSize.height/2 < subDungeon.rect.height) break;//방의 크기에 건물이 들어가면 검사 종료
                for (int i = 0; i < buildHash.Length; i++)
                {
                    if (buildHash[i] == 0) num++;//해시 배열 검사
                }
                if (num > 0) continue;//아직 모든 건물 확인하지 않았을 시 다시 검사
                if (num==0)
                {
                    isfull = true;//모든 건물을 검사했다면 검사 종료
                    break;
                }

            }
            if (!isfull)//방 사이즈에 맞는 건물이 있을 경우
            {
                int x = (int)Random.Range(subDungeon.rect.x, subDungeon.rect.xMax - buildSize.width);//현재 방 크기와 건물 크기에 맞춰 랜덤한 위치 선정
                int y = (int)Random.Range(subDungeon.rect.y, subDungeon.rect.yMax - buildSize.height/2);

                GameObject instance = Instantiate(building[buildChoice], new Vector3(x, y, 0f), Quaternion.identity) as GameObject;
                instance.transform.SetParent(transform);
                for(int i = x; i < (int)buildSize.width + x; i++)//위치 배열에 건물 정보 추가
                {
                    for(int j = y; j< (int)buildSize.height/2 + y; j++)
                        boardPositionsFloor[i, j] = instance;
                }
                
            }
        }
        else
        {
            DrawBuilding(subDungeon.left);
            DrawBuilding(subDungeon.right);
        }
    }
    public void Drawtrashcan(SubDungeon subDungeon)
    {
        
        if (subDungeon == null)
        {
            return;
        }
        if (subDungeon.IAmLeaf())
        {
            int x;
            int y;
            while (true)
            {
                x = (int)Random.Range(subDungeon.rect.x, subDungeon.rect.xMax);//현재 방 크기와 건물 크기에 맞춰 랜덤한 위치 선정
                y = (int)Random.Range(subDungeon.rect.y, subDungeon.rect.yMax);

                if (boardPositionsFloor[x, y].tag != building[0].tag) break;
            }
            GameObject instance = Instantiate(trashcan, new Vector3(x, y, -1f), Quaternion.identity) as GameObject;
            instance.transform.SetParent(transform);
            thingPositionsFloor[x, y] = instance;
        }
        else
        {
            Drawtrashcan(subDungeon.left);
            Drawtrashcan(subDungeon.right);
        }
    }
    public void DrawBlock() // 방 크기의 테두리를 블록으로 생성
    {
        for (int i = 0; i < boardColumns; i++)
        {

            GameObject instance = Instantiate(blookTile, new Vector3(0, i, 0f), Quaternion.identity) as GameObject;
            instance.transform.SetParent(transform);
            boardPositionsFloor[0, i] = instance;
        }
        for (int i = 0; i < boardColumns; i++)
        {

            GameObject instance = Instantiate(blookTile, new Vector3(boardRows - 1, i, 0f), Quaternion.identity) as GameObject;
            instance.transform.SetParent(transform);
            boardPositionsFloor[boardRows - 1, i] = instance;
        }
        for (int i = 0; i < boardRows; i++)
        {

            GameObject instance = Instantiate(blookTile, new Vector3(i, 0, 0f), Quaternion.identity) as GameObject;
            instance.transform.SetParent(transform);
            boardPositionsFloor[i, 0] = instance;
        }
        for (int i = 0; i < boardRows; i++)
        {

            GameObject instance = Instantiate(blookTile, new Vector3(i, boardColumns - 1, 0f), Quaternion.identity) as GameObject;
            instance.transform.SetParent(transform);
            boardPositionsFloor[i, boardColumns - 1] = instance;
        }
    }
    void Start()
    {
      
                SubDungeon rootSubDungeon = new SubDungeon(new Rect(0, 0, boardRows, boardColumns));
                buildHash = new int[building.Length];
                CreateBSP(rootSubDungeon);
                //rootSubDungeon.CreateRoom();
                rootSubDungeon.CreateLine();
                boardPositionsFloor = new GameObject[boardRows, boardColumns];
                thingPositionsFloor = new GameObject[boardRows, boardColumns];
                DrawRooms(rootSubDungeon);
                DrawLines(rootSubDungeon);
                DrawBuilding(rootSubDungeon);
                DrawBlock();

                DrawLamp(rootSubDungeon);
        Drawtrashcan(rootSubDungeon);

    }
    void hashClear()//해시 배열을 모두 0으로 초기화하는 함수
    {
        for (int i = 0; i < buildHash.Length; i++)
            buildHash[i] = 0;
    }

    private bool IsCar(Vector3 ve, Rect carSize)
    {
        for (int i = (int)ve.x; i <= (int)ve.x + carSize.width; i++)
        {
            for (int j = (int)ve.y; j <= (int)ve.y + carSize.height; j++)
            {
                if (i >= boardRows || j >= boardColumns) return true;

                if (thingPositionsFloor[i, j] == null) continue;

                if (thingPositionsFloor[i, j].tag == car[0].tag)
                    return true;
            }

        }
        return false;
    }
    private void SetCar(GameObject car,Vector3 ve, Rect carSize)
    {
        for(int i = (int)ve.x; i <= (int)ve.x + carSize.width; i++)
        {
            for(int j=(int)ve.y;j<=(int)ve.y + carSize.height; j++)
            {
                thingPositionsFloor[i, j] = car;
            }
        }
    }
    void Vehicle(SubDungeon subDungeon)
    {
        Rect vehiSize;
        
        if (subDungeon.isline == 1)
        {
            int[] ra = Getrandom(5, (int)subDungeon.line.x + 3, (int)subDungeon.line.xMax - 10);  //같지않은 랜덤값 // 근데 존나겹침
            Vector3 ve = new Vector3(ra[1], (int)subDungeon.line.y +1, -2f);
            if (Random.Range(0, 100) <= 50) //50% 확률로 반대차선 으로 나옴
            {
                vehiSize = car[1].GetComponent<RectTransform>().rect;
                if (!IsCar(ve, vehiSize))
                {
                    GameObject instance = Instantiate(car[1], ve, Quaternion.identity) as GameObject; // 도로에 무조건 차 하나는 있음
                    SetCar(instance, ve, vehiSize);
                }
            }
            else
            {
                ve = new Vector3(ra[1], (int)subDungeon.line.y + 5, -1f);
                vehiSize = car[1].GetComponent<RectTransform>().rect;
                if (!IsCar(ve, vehiSize))
                {
                    GameObject instance = Instantiate(car[1], ve, Quaternion.Euler(180, 0, 180)) as GameObject; // 도로에 무조건 차 하나는 있음
                    SetCar(instance, ve, vehiSize);
                }
            }
            if (subDungeon.line.xMax - subDungeon.line.x >= 70)  // 길의 길이가 20이상 일때만 버스 나옴
            {
                vehiSize = car[0].GetComponent<RectTransform>().rect;
                if (Random.Range(0, 100) <= 50) //50% 확률로 반대차선 으로 나옴
                {
                    Vector3 bus = new Vector3(ra[0], (int)subDungeon.line.y+2, -2f);
                    
                    if (!IsCar(bus, vehiSize))
                    {
                        GameObject instance2 = Instantiate(car[0], bus, Quaternion.identity) as GameObject;
                        SetCar(instance2, bus, vehiSize);
                    }
                }
                else
                {
                    Vector3 bus = new Vector3(ra[0], (int)subDungeon.line.y + 6, -1f);
                    if (!IsCar(bus, vehiSize))
                    {
                        GameObject instance2 = Instantiate(car[8], bus, Quaternion.identity) as GameObject;   //// 버스그림 새로그려야됨
                        SetCar(instance2, bus, vehiSize);
                    }
                }
            }
            if (subDungeon.line.xMax - subDungeon.line.x >= 25) // 길의 길이가 25이상이면 차가 하나 더나옴
            {
                vehiSize = car[1].GetComponent<RectTransform>().rect;
                if (Random.Range(0, 100) <= 50) //50% 확률로 반대차선 으로 나옴
                {
                    ve = new Vector3(ra[2], (int)subDungeon.line.y +1, -2f);
                    if (!IsCar(ve, vehiSize))
                    {
                        GameObject instance3 = Instantiate(car[1], ve, Quaternion.identity) as GameObject;
                        SetCar(instance3, ve, vehiSize);
                    }
                }
                else
                {
                    ve = new Vector3(ra[2], (int)subDungeon.line.y + 5, -1f);
                    if (!IsCar(ve, vehiSize))
                    {
                        GameObject instance3 = Instantiate(car[1], ve, Quaternion.Euler(180, 0, 180)) as GameObject;
                        SetCar(instance3, ve, vehiSize);
                    }
                    }
            }
            if (subDungeon.line.xMax - subDungeon.line.x >= 70) // 길의 길이가 70이상이면 스포츠카가 하나나옴
            {
                vehiSize = car[2].GetComponent<RectTransform>().rect;
                if (Random.Range(0, 100) <= 50) //50% 확률로 반대차선 으로 나옴
                {
                ve = new Vector3(ra[3], (int)subDungeon.line.y +2 , -2f);
                    if (!IsCar(ve, vehiSize))
                    {
                        GameObject instance3 = Instantiate(car[2], ve, Quaternion.identity) as GameObject;
                        SetCar(instance3, ve, vehiSize);
                    }
                }
                else
                {
                    ve = new Vector3(ra[3], (int)subDungeon.line.y + 4, -1f);
                    if (!IsCar(ve, vehiSize))
                    {                    
                         GameObject instance3 = Instantiate(car[2], ve, Quaternion.Euler(180, 0, 180)) as GameObject;
                        SetCar(instance3, ve, vehiSize);
                    }
                }
            }
            if (subDungeon.line.xMax - subDungeon.line.x >= 50) // 길의 길이가 50이상이면 트럭이 하나나옴
            {
                vehiSize = car[4].GetComponent<RectTransform>().rect;
                if (Random.Range(0, 100) <= 50) //50% 확률로 반대차선 으로 나옴
                {
                    ve = new Vector3(ra[4], (int)subDungeon.line.y + 2, -2f);
                    if (!IsCar(ve, vehiSize))
                    {
                        GameObject instance3 = Instantiate(car[3], ve, Quaternion.identity) as GameObject;
                        SetCar(instance3, ve, vehiSize);
                    }
                }
                else
                {
                    if (!IsCar(ve, vehiSize))
                    {
                        ve = new Vector3(ra[4], (int)subDungeon.line.y + 5, -1f);
                    GameObject instance3 = Instantiate(car[3], ve, Quaternion.Euler(180, 0, 180)) as GameObject;
                    SetCar(instance3, ve, vehiSize);
                }
            }
            }
            }
        if (subDungeon.isline == 2)   
            ////////// 세로 그림 아직 안그려져서 그리고나서 수정 다해야됨 방향같은거
        {
            int[] ra = Getrandom(5, (int)subDungeon.line.y+1, (int)subDungeon.line.yMax - 10);  //같지않은 랜덤값 // 근데 존나겹침
            Vector3 ve = new Vector3((int)subDungeon.line.x +1.5f, ra[1], -2f);
            vehiSize = car[4].GetComponent<RectTransform>().rect;
            if (Random.Range(0, 100) <= 50) //50% 확률로 반대차선 으로 나옴
            {
                if (!IsCar(ve, vehiSize))
                {
                    GameObject instance = Instantiate(car[4], ve, Quaternion.identity) as GameObject; // 도로에 무조건 차 하나는 있음
                    SetCar(instance, ve, vehiSize);
                }
                }
            else
            {
                ve = new Vector3((int)subDungeon.line.x + 4.5f, ra[1], -1f);
                if (!IsCar(ve, vehiSize))
                {
                    GameObject instance = Instantiate(car[5], ve, Quaternion.identity) as GameObject; // 도로에 무조건 차 하나는 있음
                    SetCar(instance, ve, vehiSize);
                }
            }
            if (subDungeon.line.yMax - subDungeon.line.y >= 60)  // 길의 길이가 20이상 일때만 버스 나옴
            {
                vehiSize = car[6].GetComponent<RectTransform>().rect;
                if (Random.Range(0, 100) <= 50) //50% 확률로 반대차선 으로 나옴
                {
                    Vector3 bus = new Vector3((int)subDungeon.line.x + 1.5f, ra[0], -2f);
                    if (!IsCar(bus, vehiSize))
                    {
                        GameObject instance2 = Instantiate(car[6], bus, Quaternion.identity) as GameObject;
                        SetCar(instance2, bus, vehiSize);
                    }
                    }
                else
                {
                    vehiSize = car[7].GetComponent<RectTransform>().rect;
                    Vector3 bus = new Vector3((int)subDungeon.line.x +4.5f, ra[0], -1f);
                    if (!IsCar(bus, vehiSize))
                    {
                        GameObject instance2 = Instantiate(car[7], bus, Quaternion.identity) as GameObject;
                        SetCar(instance2, bus, vehiSize);
                    }
                    }
            }
            if (subDungeon.line.yMax - subDungeon.line.y >= 25) // 길의 길이가 25이상이면 차가 하나 더나옴
            {
                vehiSize = car[4].GetComponent<RectTransform>().rect;
                if (Random.Range(0, 100) <= 50) //50% 확률로 반대차선 으로 나옴
                {
                    ve = new Vector3((int)subDungeon.line.x +1.5f, ra[2], -2f);
                    if (!IsCar(ve, vehiSize))
                    {
                        GameObject instance3 = Instantiate(car[4], ve, Quaternion.identity) as GameObject;
                        SetCar(instance3, ve, vehiSize);
                    }
                    }
                else
                {
                    ve = new Vector3((int)subDungeon.line.x + 4.5f, ra[2], -1f);
                    if (!IsCar(ve, vehiSize))
                    {
                        GameObject instance3 = Instantiate(car[5], ve, Quaternion.Euler(180, 0, 180)) as GameObject;
                        SetCar(instance3, ve, vehiSize);
                    }
                    }
            }
            if (subDungeon.line.yMax - subDungeon.line.y >= 70) // 길의 길이가 70이상이면 스포츠카가 하나나옴
            {
                vehiSize = car[11].GetComponent<RectTransform>().rect;
                if (Random.Range(0, 100) <= 50) //50% 확률로 반대차선 으로 나옴
                {
                    ve = new Vector3((int)subDungeon.line.x +1.5f, ra[3], -2f);
                    if (!IsCar(ve, vehiSize))
                    {
                        GameObject instance3 = Instantiate(car[11], ve, Quaternion.identity) as GameObject;
                        SetCar(instance3, ve, vehiSize);
                    }
                    }
                else
                {
                    ve = new Vector3((int)subDungeon.line.x + 4.5f, ra[3], -1f);
                    if (!IsCar(ve, vehiSize))
                    {
                        GameObject instance3 = Instantiate(car[12], ve, Quaternion.Euler(180, 0, 180)) as GameObject;
                        SetCar(instance3, ve, vehiSize);
                    }
                }
            }
            if (subDungeon.line.yMax - subDungeon.line.y >= 40) // 길의 길이가 40이상이면 트럭이 하나나옴
            {
                vehiSize = car[9].GetComponent<RectTransform>().rect;
                if (Random.Range(0, 100) <= 50) //50% 확률로 반대차선 으로 나옴
                {
                    ve = new Vector3(subDungeon.line.x +1.5f, ra[4], -2f);
                    if (!IsCar(ve, vehiSize))
                    {
                        GameObject instance3 = Instantiate(car[9], ve, Quaternion.identity) as GameObject;
                        SetCar(instance3, ve, vehiSize);
                    }
                }
                else
                {
                    ve = new Vector3(subDungeon.line.x + 4.5f, ra[4], -1f);
                    if (!IsCar(ve, vehiSize))
                    {
                        GameObject instance3 = Instantiate(car[10], ve, Quaternion.Euler(180, 0, 180)) as GameObject;
                        SetCar(instance3, ve, vehiSize);
                    }
                }
            }
        }
    }
    public int[] Getrandom(int length, int min, int max) // 같지않은 랜덤값 만들기
    {
        int[] randarray = new int[length];
        bool isSame;
        for (int i = 0; i < length; ++i)
        {
            while (true)
            {
                randarray[i] = Random.Range(min, max);
                isSame = false;
                if (i == 0) break;
                for (int j = 0; j < i; ++j)
                {
                    
                    if (i == j) continue;
                    if (randarray[i] - randarray[j] < 10)
                    {
                        continue;
                    }
                    if (randarray[j] == randarray[i])
                    {
                        isSame = true;
                        break;
                    }
                }
                if (!isSame) break;
            }
        }
        /*
        Array.Sort(randarray);
        for (int i = 0; i < length-1; ++i)
        {
                if(randarray[i+1] - randarray[i] < 2)
            {
                randarray[i+1] = randarray[i] + 5;
            }
        }
        */
        return randarray;
    }
}