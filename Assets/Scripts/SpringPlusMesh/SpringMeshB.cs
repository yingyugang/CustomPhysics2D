using BlueNoah.Event;
using System.Collections.Generic;
using UnityEngine;

namespace BlueNoah 
{ 

    //顺时针，逆时针判断顶点是否越界了，越界的话相应的顶点向移动方向移动。
    public class SpringMeshB : MonoBehaviour
    {

        class JointEntity
        {
            public Transform transform;
            public List<int[]> connects = new List<int[]>();
            public HashSet<JointEntity> connectJointEntities = new HashSet<JointEntity>();
            public int index;
            public int checkIndex;
        }


        [SerializeField]
        Texture2D texture2D;
        [SerializeField]
        GameObject prefab;
        [SerializeField]
        Transform center;
        [SerializeField]
        Camera myCamera;

        GameObject meshGo;
        int pixelPerUnit = 20;
        int xCount;
        int yCount;

        Mesh mesh;
        Vector3[] vertices;
        int[] triangles;
        Transform[] jointTrans;

        JointEntity[] jointEntities;
        Dictionary<Transform, JointEntity> jointDic = new Dictionary<Transform, JointEntity>();

        private void Awake()
        {
            meshGo = MeshUtility.CreateTilePlane(texture2D, pixelPerUnit, out xCount, out yCount);
            meshGo.transform.position = center.position;
            CombineMeshToGameObject();

            EasyInput.Instance.AddGlobalListener(Event.TouchType.TouchBegin, OnTouchBegin);
            EasyInput.Instance.AddGlobalListener(Event.TouchType.Touch, OnTouch);
            EasyInput.Instance.AddGlobalListener(Event.TouchType.TouchEnd, OnTouchEnd);
        }


        Transform select;
        Vector3 selectOffset;
        void OnTouchBegin(EventData eventData)
        {
            //Physics2D.Raycast(new Vector2(camera.ScreenToWorldPoint(Input.mousePosition).x,camera.ScreenToWorldPoint(Input.mousePosition).y), Vector2.Zero, 0f);
            var pos = ScreenPositionToOrthograhicCameraPosition(eventData);
            float distance = 0.5f;
            var colls = Physics2D.OverlapCircleAll(pos, distance);

            foreach (var item in colls)
            {
                var dis = Vector2.Distance(item.transform.position,pos);
                if (dis < distance)
                {
                    select = item.transform;
                    distance = dis;
                }
            }
            if (select != null)
            {
                selectOffset =  pos - select.position;
                prePos = pos;
            }
            foreach (var item in jointTrans)
            {
                var springJoints = item.GetComponents<SpringJoint2D>();
                foreach (var joint2D in springJoints)
                {
                    joint2D.autoConfigureDistance = false;
                }
            }
        }


        Vector3 prePos;
        void OnTouch(EventData eventData)
        {
            if (select != null)
            {
                openList.Clear();
                openIndex = 0;
                var pos = ScreenPositionToOrthograhicCameraPosition(eventData);
                JointEntity entity = jointDic[select];
                jointEntities[entity.index].transform.position = pos - selectOffset - center.position;

                openList.Add(entity);
                Extrusion(pos);
                prePos = pos;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = jointTrans[i].position - center.position;
                    //mesh.RecalculateBounds();
                }
                mesh.vertices = vertices;
            }
        }

        List<JointEntity> openList = new List<JointEntity>();
        int openIndex = 0;
        int checkIndex = 0;
        void Extrusion(Vector3 pos)
        {
            checkIndex++;
            if (checkIndex == int.MaxValue)
            {
                checkIndex = 0;
            }
            while (openIndex < openList.Count)
            {
                JointEntity entity = openList[openIndex];
                entity.checkIndex = checkIndex;
                openIndex++;
                var connects = entity.connects;
                for (int i = 0; i < connects.Count; i++)
                {
                    //三角形的Cross如何反了，变成逆时针的话说明顶点重复了。
                    var cross = Vector3.Cross((jointEntities[connects[i][2]].transform.position - jointEntities[connects[i][0]].transform.position).normalized, (jointEntities[connects[i][1]].transform.position - jointEntities[connects[i][0]].transform.position).normalized);
                    if (cross.z <= 0)
                    {
                        if (connects[i][0] != entity.index)
                        {
                            if (jointEntities[connects[i][0]].checkIndex!=checkIndex)
                            {
                                jointEntities[connects[i][0]].transform.position += (pos - prePos);
                                openList.Add(jointEntities[connects[i][0]]);
                            }
                        }
                        if (connects[i][1] != entity.index)
                        {
                            if (jointEntities[connects[i][1]].checkIndex != checkIndex)
                            {
                                jointEntities[connects[i][1]].transform.position += (pos - prePos);
                                openList.Add(jointEntities[connects[i][1]]);
                            }
                        }
                        if (connects[i][2] != entity.index)
                        {
                            if (jointEntities[connects[i][2]].checkIndex != checkIndex)
                            {
                                jointEntities[connects[i][2]].transform.position += (pos - prePos);
                                openList.Add(jointEntities[connects[i][2]]);
                            }
                        }
                    }
                }

            }
        }


        void OnTouchEnd(EventData eventData)
        {
            if (select != null)
            {
                //select.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY;
                select = null;
            }
        }

        Vector3 ScreenPositionToOrthograhicCameraPosition(EventData eventData)
        {
            float sizePerPixel = myCamera.orthographicSize * 2 / Screen.height;
            float x = (eventData.currentTouch.touch.position.x - Screen.width / 2) * sizePerPixel;
            float y = (eventData.currentTouch.touch.position.y - Screen.height / 2) * sizePerPixel;
            return myCamera.transform.position + myCamera.transform.up * y + myCamera.transform.right * x;
        }

        void CombineMeshToGameObject()
        {
            mesh = meshGo.GetComponent<MeshFilter>().mesh;
            vertices = mesh.vertices;
            triangles = mesh.triangles;
            jointTrans = new Transform[vertices.Length];
            jointEntities = new JointEntity[vertices.Length];

            for (int i = 0; i < yCount; i++)
            {
                for (int j = 0; j < xCount; j++)
                {
                    var index = i * xCount + j;
                    var vertic = vertices[index];
                    GameObject go = Instantiate(prefab, vertic, Quaternion.identity);
                    go.transform.localScale = Vector3.one * pixelPerUnit / 100f;
                    jointTrans[index] = go.transform;
                    jointTrans[index].position = vertic + center.position;
                    jointEntities[index] = new JointEntity();
                    jointEntities[index].transform = jointTrans[index];
                    jointEntities[index].index = index;
                    jointDic.Add(jointTrans[index], jointEntities[index]);
                }
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                jointEntities[triangles[i]].connects.Add(new int[] { triangles[i], triangles[i + 1], triangles[i + 2] });
                jointEntities[triangles[i]].connectJointEntities.Add(jointEntities[triangles[i + 1]]);
                jointEntities[triangles[i]].connectJointEntities.Add(jointEntities[triangles[i + 2]]);

                jointEntities[triangles[i + 1]].connects.Add(new int[] { triangles[i], triangles[i + 1], triangles[i + 2] });
                jointEntities[triangles[i + 1]].connectJointEntities.Add(jointEntities[triangles[i]]);
                jointEntities[triangles[i + 1]].connectJointEntities.Add(jointEntities[triangles[i + 2]]);

                jointEntities[triangles[i + 2]].connects.Add(new int[] { triangles[i], triangles[i + 1], triangles[i + 2] });
                jointEntities[triangles[i + 2]].connectJointEntities.Add(jointEntities[triangles[i]]);
                jointEntities[triangles[i + 2]].connectJointEntities.Add(jointEntities[triangles[i + 1]]);
            }
        }


        private void Update()
        {
           
        }
    }
}
