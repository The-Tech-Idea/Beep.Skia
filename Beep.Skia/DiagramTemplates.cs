using SkiaSharp;
using System;
using System.Collections.Generic;
using Beep.Skia.Model;
using Beep.Skia.Serialization;

namespace Beep.Skia
{
    /// <summary>
    /// Provides pre-built diagram templates as DiagramDto objects that can be loaded via DrawingManager.LoadFromDto().
    /// </summary>
    public static class DiagramTemplates
    {
        private static string AQN(string ns, string cls) => $"Beep.Skia.{ns}.{cls}, Beep.Skia.{ns}";

        private static ComponentDto MakeComp(string fullName, float x, float y, float w, float h, string name = null, params (string key, string val)[] props)
        {
            var c = new ComponentDto { Type = fullName, X = x, Y = y, Width = w, Height = h, Name = name ?? "Component" };
            foreach (var (k, v) in props)
            {
                if (v != null) c.PropertyBag[k] = v;
            }
            return c;
        }

        // ── FlowChart: Order Validation ──────────────────────────────────────

        public static DiagramDto FlowChartOrderValidation()
        {
            var dto = new DiagramDto();
            var start = MakeComp(AQN("FlowChart", "StartEndNode"), 250, 40, 100, 40, "Start", ("Title", "Start"));
            var receive = MakeComp(AQN("FlowChart", "ProcessNode"), 220, 110, 160, 50, "Receive Order", ("Title", "Receive Order"));
            var validate = MakeComp(AQN("FlowChart", "DecisionNode"), 230, 190, 140, 80, "Valid?", ("Title", "Valid?"));
            var process = MakeComp(AQN("FlowChart", "ProcessNode"), 220, 300, 160, 50, "Process Payment", ("Title", "Process Payment"));
            var ship = MakeComp(AQN("FlowChart", "ProcessNode"), 220, 380, 160, 50, "Ship Order", ("Title", "Ship Order"));
            var reject = MakeComp(AQN("FlowChart", "ProcessNode"), 430, 200, 140, 50, "Notify Customer", ("Title", "Notify Customer"));
            var end = MakeComp(AQN("FlowChart", "StartEndNode"), 250, 470, 100, 40, "End", ("Title", "End"));
            dto.Components.AddRange(new[] { start, receive, validate, process, ship, reject, end });

            // Connections — GUIDs are assigned during LoadFromDto via registry lookup
            dto.Lines = new List<LineDto>
            {
                Conn(0, 1),
                Conn(1, 2),
                Conn(2, 3, "Yes"),
                Conn(2, 5, "No"),
                Conn(3, 4),
                Conn(4, 6),
                Conn(5, 6)
            };
            return dto;
        }

        // ── ERD: E-Commerce Schema ───────────────────────────────────────────

        public static DiagramDto ERDEcommerce()
        {
            var dto = new DiagramDto();
            var customers = MakeComp(AQN("ERD", "ERDEntity"), 40, 60, 200, 160, "Customers",
                ("EntityName", "Customers"), ("RowsText", "*Id:int\n Name:varchar(100)\n Email:varchar(255)\n CreatedAt:datetime"));
            var orders = MakeComp(AQN("ERD", "ERDEntity"), 340, 60, 220, 180, "Orders",
                ("EntityName", "Orders"), ("RowsText", "*Id:int\n CustomerId:int FK\n OrderDate:datetime\n Total:decimal\n Status:varchar(20)"));
            var products = MakeComp(AQN("ERD", "ERDEntity"), 650, 60, 200, 140, "Products",
                ("EntityName", "Products"), ("RowsText", "*Id:int\n Name:varchar(200)\n Price:decimal\n Stock:int"));
            var items = MakeComp(AQN("ERD", "ERDEntity"), 340, 300, 240, 180, "OrderItems",
                ("EntityName", "OrderItems"), ("RowsText", "*Id:int\n OrderId:int FK\n ProductId:int FK\n Quantity:int\n UnitPrice:decimal"));
            dto.Components.AddRange(new[] { customers, orders, products, items });
            dto.Lines = new List<LineDto>
            {
                ERDRel("Customers", "Orders", "places", ERDMultiplicity.One, ERDMultiplicity.Many),
                ERDRel("Orders", "OrderItems", "contains", ERDMultiplicity.One, ERDMultiplicity.Many),
                ERDRel("Products", "OrderItems", "has", ERDMultiplicity.One, ERDMultiplicity.Many)
            };
            return dto;
        }

        // ── ETL: Data Warehouse Pipeline ─────────────────────────────────────

        public static DiagramDto ETLPipeline()
        {
            var dto = new DiagramDto();
            var src = MakeComp(AQN("ETL", "ETLSource"), 50, 200, 120, 60, "SQL Server");
            var extract = MakeComp(AQN("ETL", "ETLTransform"), 230, 200, 120, 60, "Extract Orders");
            var filter = MakeComp(AQN("ETL", "ETLFilter"), 410, 120, 120, 60, "Active Only");
            var join = MakeComp(AQN("ETL", "ETLJoin"), 410, 260, 120, 60, "Join Products");
            var agg = MakeComp(AQN("ETL", "ETLAggregate"), 590, 140, 120, 60, "Daily Totals");
            var tgt = MakeComp(AQN("ETL", "ETLTarget"), 770, 200, 130, 60, "DW FactTable");
            dto.Components.AddRange(new[] { src, extract, filter, join, agg, tgt });
            dto.Lines = new List<LineDto>
            {
                Conn(0, 1, "RawData"), Conn(1, 2, "Filtered"), Conn(1, 3, "Products"),
                Conn(2, 4, "Clean"), Conn(3, 4, "Joined"), Conn(4, 5, "Aggregated")
            };
            return dto;
        }

        // ── DFD: Order System Level 0 ────────────────────────────────────────

        public static DiagramDto DFDOrderSystem()
        {
            var dto = new DiagramDto();
            var cust = MakeComp(AQN("DFD", "DFDExternalEntity"), 40, 200, 120, 70, "Customer");
            var proc = MakeComp(AQN("DFD", "DFDProcess"), 280, 180, 160, 80, "Process Order");
            var store = MakeComp(AQN("DFD", "DFDDataStore"), 540, 200, 120, 80, "OrdersDB");
            var ship = MakeComp(AQN("DFD", "DFDExternalEntity"), 760, 200, 120, 70, "Shipping");
            dto.Components.AddRange(new[] { cust, proc, store, ship });
            dto.Lines = new List<LineDto>
            {
                Conn(0, 1, "Order"), Conn(1, 2, "Validate&Store"), Conn(2, 1, "Confirmed"),
                Conn(1, 3, "Shipment"), Conn(3, 1, "Tracking")
            };
            return dto;
        }

        // ── UML: Class Diagram ───────────────────────────────────────────────

        public static DiagramDto UMLClassDiagram()
        {
            var dto = new DiagramDto();
            var user = MakeComp(AQN("UML", "UMLClass"), 50, 60, 180, 140, "User",
                ("ClassName", "User"), ("AttributesText", "- id : int\n- name : string\n- email : string\n- role : string"),
                ("MethodsText", "+ login() : bool\n+ logout() : void"));
            var order = MakeComp(AQN("UML", "UMLClass"), 330, 60, 180, 160, "Order",
                ("ClassName", "Order"), ("AttributesText", "- id : int\n- date : DateTime\n- status : string\n- total : decimal"),
                ("MethodsText", "+ calculateTotal() : decimal\n+ updateStatus() : void"));
            var product = MakeComp(AQN("UML", "UMLClass"), 610, 60, 180, 120, "Product",
                ("ClassName", "Product"), ("AttributesText", "- id : int\n- name : string\n- price : decimal\n- stock : int"),
                ("MethodsText", "+ isInStock() : bool"));
            var paySvc = MakeComp(AQN("UML", "UMLInterface"), 330, 300, 180, 100, "IPaymentService",
                ("ClassName", "IPaymentService"), ("MethodsText", "+ processPayment() : bool\n+ refund() : bool"));
            dto.Components.AddRange(new[] { user, order, product, paySvc });
            dto.Lines = new List<LineDto>
            {
                UMLAssoc(AQN("UML", "UMLClass"), AQN("UML", "UMLClass"), "places"),
                UMLInherit(AQN("UML", "UMLInterface"))
            };
            return dto;
        }

        // ── Network: Social Graph ────────────────────────────────────────────

        public static DiagramDto NetworkSocialGraph()
        {
            var dto = new DiagramDto();
            float[][] positions = { new[] { 400f, 150f }, new[] { 250f, 100f }, new[] { 550f, 80f },
                new[] { 200f, 250f }, new[] { 400f, 280f }, new[] { 600f, 220f },
                new[] { 300f, 380f }, new[] { 500f, 360f }, new[] { 400f, 450f } };
            string[] names = { "Alice", "Bob", "Carol", "Dave", "Eve", "Frank", "Grace", "Heidi", "Ivan" };
            var nodes = new List<ComponentDto>();
            for (int i = 0; i < names.Length; i++)
                nodes.Add(MakeComp(AQN("Network", "NetworkNode"), positions[i][0], positions[i][1], 60, 60, names[i], ("Label", names[i])));
            dto.Components.AddRange(nodes);

            int[][] edges = { new[] { 0, 1 }, new[] { 0, 2 }, new[] { 0, 4 }, new[] { 1, 3 },
                new[] { 2, 5 }, new[] { 3, 6 }, new[] { 4, 5 }, new[] { 4, 7 },
                new[] { 5, 8 }, new[] { 6, 8 }, new[] { 7, 8 }, new[] { 3, 4 } };
            foreach (var e in edges)
                dto.Lines.Add(Conn(e[0], e[1]));
            return dto;
        }

        // ── MindMap: Product Strategy ────────────────────────────────────────

        public static DiagramDto MindMapProductStrategy()
        {
            var dto = new DiagramDto();
            var root = MakeComp(AQN("MindMap", "CentralNode"), 260, 170, 80, 80, "Product", ("Title", "Product"));
            // Level 1 topics in a circle
            (string name, float angle)[] topics = { ("Marketing", -90), ("Engineering", -30), ("Sales", 30), ("Support", 90),
                ("Finance", 150), ("UX Design", 210) };
            var topicNodes = new List<ComponentDto>();
            float r = 160f, cx = 300f, cy = 210f;
            foreach (var (name, angle) in topics)
            {
                float rad = (float)(angle * Math.PI / 180.0);
                topicNodes.Add(MakeComp(AQN("MindMap", "TopicNode"),
                    cx + r * (float)Math.Cos(rad) - 60, cy + r * (float)Math.Sin(rad) - 25, 120, 50, name, ("Title", name)));
            }
            dto.Components.Add(root);
            dto.Components.AddRange(topicNodes);
            for (int i = 0; i < topicNodes.Count; i++)
                dto.Lines.Add(Conn(0, i + 1));
            return dto;
        }

        // ── StateMachine: Login Process ──────────────────────────────────────

        public static DiagramDto StateMachineLogin()
        {
            var dto = new DiagramDto();
            var init = MakeComp(AQN("StateMachine", "InitialStateNode"), 80, 180, 40, 40, "Initial");
            var idle = MakeComp(AQN("StateMachine", "StateNode"), 200, 165, 140, 60, "Idle", ("Title", "Idle"));
            var auth = MakeComp(AQN("StateMachine", "StateNode"), 420, 110, 140, 60, "Authenticating", ("Title", "Authenticating"));
            var loggedIn = MakeComp(AQN("StateMachine", "StateNode"), 420, 230, 140, 60, "LoggedIn", ("Title", "Logged In"));
            var locked = MakeComp(AQN("StateMachine", "StateNode"), 420, 350, 140, 60, "Locked", ("Title", "Locked"));
            var final = MakeComp(AQN("StateMachine", "FinalStateNode"), 660, 250, 40, 40, "Final");
            dto.Components.AddRange(new[] { init, idle, auth, loggedIn, locked, final });
            dto.Lines = new List<LineDto>
            {
                Conn(0, 1), Conn(1, 2, "Login"), Conn(2, 3, "Success"), Conn(2, 4, "3 failures"),
                Conn(3, 5, "Logout"), Conn(4, 1, "Timeout"), Conn(1, 5, "Close")
            };
            return dto;
        }

        // ── Business: Purchase Approval ──────────────────────────────────────

        public static DiagramDto BusinessPurchaseApproval()
        {
            var dto = new DiagramDto();
            var start = MakeComp(AQN("Business", "StartEvent"), 80, 200, 60, 60, "Start");
            var submit = MakeComp(AQN("Business", "BusinessTask"), 220, 185, 140, 60, "Submit PO", ("TaskName", "Submit PO"));
            var review = MakeComp(AQN("Business", "Decision"), 440, 185, 80, 80, "Approved?", ("DecisionName", "Approved?"));
            var proc = MakeComp(AQN("Business", "BusinessTask"), 600, 100, 140, 60, "Process Order", ("TaskName", "Process Order"));
            var notify = MakeComp(AQN("Business", "BusinessTask"), 600, 260, 140, 60, "Notify Requester", ("TaskName", "Notify Requester"));
            var end = MakeComp(AQN("Business", "EndEvent"), 820, 200, 60, 60, "End");
            dto.Components.AddRange(new[] { start, submit, review, proc, notify, end });
            dto.Lines = new List<LineDto>
            {
                Conn(0, 1), Conn(1, 2), Conn(2, 3, "Yes"), Conn(2, 4, "No"), Conn(3, 5), Conn(4, 5)
            };
            return dto;
        }

        // ── PM: Sprint Plan ─────────────────────────────────────────────────

        public static DiagramDto PMSprintPlan()
        {
            var dto = new DiagramDto();
            var plan = MakeComp(AQN("PM", "TaskNode"), 80, 60, 140, 60, "Sprint Planning", ("Title", "Sprint Planning"), ("PercentComplete", "100"));
            var dev1 = MakeComp(AQN("PM", "TaskNode"), 280, 40, 140, 60, "API Development", ("Title", "API Dev"), ("PercentComplete", "70"));
            var dev2 = MakeComp(AQN("PM", "TaskNode"), 280, 130, 140, 60, "UI Development", ("Title", "UI Dev"), ("PercentComplete", "40"));
            var review = MakeComp(AQN("PM", "TaskNode"), 480, 85, 140, 60, "Code Review", ("Title", "Code Review"), ("PercentComplete", "0"));
            var test = MakeComp(AQN("PM", "TaskNode"), 680, 85, 140, 60, "QA Testing", ("Title", "QA Testing"), ("PercentComplete", "0"));
            var release = MakeComp(AQN("PM", "MilestoneNode"), 680, 200, 120, 50, "Release v2.0", ("Title", "Release v2.0"));
            dto.Components.AddRange(new[] { plan, dev1, dev2, review, test, release });
            dto.Lines = new List<LineDto>
            {
                Conn(0, 1), Conn(0, 2), Conn(1, 3), Conn(2, 3), Conn(3, 4), Conn(4, 5)
            };
            return dto;
        }

        // ── All templates list ───────────────────────────────────────────────

        public static IReadOnlyDictionary<string, Func<DiagramDto>> All => new Dictionary<string, Func<DiagramDto>>
        {
            ["FlowChart: Order Validation"] = FlowChartOrderValidation,
            ["ERD: E-Commerce Schema"] = ERDEcommerce,
            ["ETL: Data Warehouse Pipeline"] = ETLPipeline,
            ["DFD: Order System Level 0"] = DFDOrderSystem,
            ["UML: Class Diagram"] = UMLClassDiagram,
            ["Network: Social Graph"] = NetworkSocialGraph,
            ["MindMap: Product Strategy"] = MindMapProductStrategy,
            ["StateMachine: Login Process"] = StateMachineLogin,
            ["Business: Purchase Approval"] = BusinessPurchaseApproval,
            ["PM: Sprint Plan"] = PMSprintPlan
        };

        // ── Helpers ──────────────────────────────────────────────────────────

        private static LineDto Conn(int srcIdx, int dstIdx, string label = null)
        {
            return new LineDto
            {
                StartPointId = Guid.Empty,  // Placeholder — resolved during LoadFromDto renumbering
                EndPointId = Guid.Empty,
                ShowStartArrow = false,
                ShowEndArrow = true,
                Label1 = label
            };
        }

        private static LineDto ERDRel(string srcName, string dstName, string label, ERDMultiplicity startMul, ERDMultiplicity endMul)
        {
            return new LineDto
            {
                StartPointId = Guid.Empty,
                EndPointId = Guid.Empty,
                ShowStartArrow = false,
                ShowEndArrow = true,
                Label1 = label,
                StartMultiplicity = (int)startMul,
                EndMultiplicity = (int)endMul
            };
        }

        private static LineDto UMLAssoc(string srcType, string dstType, string label)
        {
            return new LineDto { StartPointId = Guid.Empty, EndPointId = Guid.Empty, ShowStartArrow = false, ShowEndArrow = true, Label1 = label };
        }

        private static LineDto UMLInherit(string type)
        {
            return new LineDto { StartPointId = Guid.Empty, EndPointId = Guid.Empty, ShowStartArrow = false, ShowEndArrow = true, Label1 = "extends" };
        }
    }

    internal static class ListExtensions
    {
        public static void AddRange<T>(this List<T> list, IEnumerable<T> items)
        {
            foreach (var item in items) list.Add(item);
        }
    }
}
