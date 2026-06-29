import { useState, useEffect, useRef } from "react";
import { 
  Users, UserCheck, Calendar, DollarSign, TrendingUp, Download, 
  Eye, CheckCircle, XCircle, AlertTriangle, Trash2, FileText, 
  Shield, Clock, Star, MessageSquare, Activity, 
  Settings, LogOut, MapPin, Filter, Search, Plus,
  Heart, Edit, Ban, Check, X, Phone, BarChart3, Sparkles, Clock as ClockIcon, Tag
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import Navbar from "@/components/Navbar";
import { toast } from "sonner";
import { adminApi } from "@/shared/services/api-admin";
import { authStorage } from "@/shared/lib/auth";

const sidebarItems = ["Overview", "Users", "Buddies", "Bookings", "Reviews", "Revenue", "Reports", "Settings"];

// ---- Initial data ----
const initialBookings = [
  { id: "BK001", user: "John Doe", experience: "Nile Sunset Cruise", status: "pending", amount: 45, date: "2024-12-15" },
  { id: "BK002", user: "Sarah Smith", experience: "Cairo Food Tour", status: "confirmed", amount: 35, date: "2024-12-14" },
  { id: "BK003", user: "Mike Johnson", experience: "Desert Safari", status: "confirmed", amount: 80, date: "2024-12-13" },
  { id: "BK004", user: "Emma Wilson", experience: "Alexandria History Walk", status: "pending", amount: 30, date: "2024-12-12" },
  { id: "BK005", user: "James Brown", experience: "Diving Experience", status: "confirmed", amount: 65, date: "2024-12-11" },
];

const initialReviews = [
  { id: 1, reviewer: "Anonymous", target: "Cairo Food Tour", reason: "Inappropriate content", date: "Dec 14, 2024", rating: 1 },
  { id: 2, reviewer: "John D.", target: "Ahmed Hassan", reason: "Spam", date: "Dec 13, 2024", rating: 2 },
];

type ApprovalStatus = "pending" | "approved" | "rejected";

interface ApprovalItem {
  id: number;
  name: string;
  type: string;
  location: string;
  photo: string;
  status: ApprovalStatus;
}

const initialApprovals: ApprovalItem[] = [
  { id: 1, name: "Youssef Ahmed", type: "buddy", location: "Hurghada", photo: "https://randomuser.me/api/portraits/men/10.jpg", status: "pending" },
  { id: 2, name: "Traditional Cooking Class", type: "experience", location: "Cairo", photo: "https://images.pexels.com/photos/262978/pexels-photo-262978.jpeg?w=150&h=150&fit=crop", status: "pending" },
  { id: 3, name: "Mariam Hassan", type: "buddy", location: "Luxor", photo: "https://randomuser.me/api/portraits/women/12.jpg", status: "pending" },
];

const initialUsers = [
  { id: 1, name: "Ahmed Hassan", email: "ahmed@example.com", role: "Buddy", status: "active", joined: "Jan 2024", avatar: "https://randomuser.me/api/portraits/men/1.jpg" },
  { id: 2, name: "Sara Mohamed", email: "sara@example.com", role: "User", status: "active", joined: "Feb 2024", avatar: "https://randomuser.me/api/portraits/women/2.jpg" },
  { id: 3, name: "John Doe", email: "john@example.com", role: "User", status: "inactive", joined: "Mar 2024", avatar: "https://randomuser.me/api/portraits/men/3.jpg" },
];

const initialBuddies = [
  { id: 1, name: "Ahmed Hassan", rating: 4.9, tours: 127, status: "active", earning: 1250, avatar: "https://randomuser.me/api/portraits/men/1.jpg", location: "Luxor", phone: "+20 123 456 789" },
  { id: 2, name: "Sara Mohamed", rating: 4.8, tours: 89, status: "active", earning: 980, avatar: "https://randomuser.me/api/portraits/women/2.jpg", location: "Cairo", phone: "+20 234 567 890" },
  { id: 3, name: "Omar Ali", rating: 4.7, tours: 56, status: "pending", earning: 450, avatar: "https://randomuser.me/api/portraits/men/3.jpg", location: "Aswan", phone: "+20 345 678 901" },
];

const Admin = () => {
  const navigate = useNavigate();
  const [activeSidebar, setActiveSidebar] = useState("Buddies");
  const [bookingsList, setBookingsList] = useState(initialBookings);
  const [reviewsList, setReviewsList] = useState(initialReviews);
  const [approvalsList, setApprovalsList] = useState<ApprovalItem[]>(initialApprovals);
  const [usersList, setUsersList] = useState(initialUsers);
  const [buddiesList, setBuddiesList] = useState(initialBuddies);
  const [searchQuery, setSearchQuery] = useState("");
  const [userSearchQuery, setUserSearchQuery] = useState("");
  const [showReportModal, setShowReportModal] = useState(false);
  const [showAddExpModal, setShowAddExpModal] = useState(false);
  const [expForm, setExpForm] = useState({
    name: "", category: "Historical", location: "", price: 0, duration: "2 hours", description: "", image: "", highlights: ""
  });

  const [likedBuddies, setLikedBuddies] = useState<number[]>(() => {
    const saved = localStorage.getItem("admin_liked_buddies");
    return saved ? JSON.parse(saved) : [];
  });

  const [editingBuddy, setEditingBuddy] = useState<any>(null);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showAddModal, setShowAddModal] = useState(false);
  const [newBuddy, setNewBuddy] = useState({
    name: "",
    rating: 4.5,
    tours: 0,
    earning: 0,
    location: "",
    phone: ""
  });

  const [editingUser, setEditingUser] = useState<any>(null);
  const [showUserEditModal, setShowUserEditModal] = useState(false);
  const apiUserIds = useRef<Record<string, string>>({});

  const [emailNotifications, setEmailNotifications] = useState(false);
  const [autoApproveBuddies, setAutoApproveBuddies] = useState(false);

  useEffect(() => {
    const storedEmail = localStorage.getItem("admin_email_notifications");
    if (storedEmail !== null) setEmailNotifications(storedEmail === "true");
    const storedAuto = localStorage.getItem("admin_auto_approve_buddies");
    if (storedAuto !== null) setAutoApproveBuddies(storedAuto === "true");
  }, []);

  useEffect(() => {
    const storedBookings = localStorage.getItem("admin_bookings");
    if (storedBookings) setBookingsList(JSON.parse(storedBookings));
    const storedReviews = localStorage.getItem("admin_reviews");
    if (storedReviews) setReviewsList(JSON.parse(storedReviews));
    const storedApprovals = localStorage.getItem("admin_approvals");
    if (storedApprovals) setApprovalsList(JSON.parse(storedApprovals));
    const storedUsers = localStorage.getItem("admin_users");
    if (storedUsers) setUsersList(JSON.parse(storedUsers));
    const storedBuddies = localStorage.getItem("admin_buddies");
    if (storedBuddies) setBuddiesList(JSON.parse(storedBuddies));
  }, []);

  useEffect(() => {
    if (!authStorage.isAuthenticated()) return;
    adminApi.getUsers()
      .then(apiUsers => {
        const map: Record<string, string> = {};
        const mapped = apiUsers.map(u => {
          map[u.email.toLowerCase()] = u.userId;
          return {
            id: parseInt(u.userId.replace(/-/g, "").slice(0, 8), 16) || Math.random(),
            name: u.fullName,
            email: u.email,
            role: u.role === "Traveler" ? "User" : u.role === "LocalBuddy" ? "Buddy" : u.role || "User",
            status: u.isActive ? "active" as const : "inactive" as const,
            joined: new Date().toLocaleString("en-US", { month: "short", year: "numeric" }),
            avatar: `https://randomuser.me/api/portraits/${Math.random() > 0.5 ? "men" : "women"}/${Math.floor(Math.random() * 50) + 1}.jpg`,
          };
        });
        apiUserIds.current = map;
        setUsersList(prev => {
          const existing = new Set(prev.map(u => u.email));
          const newOnes = mapped.filter(u => !existing.has(u.email));
          return [...newOnes, ...prev];
        });
      })
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (!authStorage.isAuthenticated()) return;
    adminApi.getExperiences()
      .then(apiExps => {
        const pending = apiExps.filter(e => e.approvalStatus === "Pending");
        if (pending.length > 0) {
          const mapped = pending.map(e => ({
            id: parseInt(e.id.replace(/-/g, "").slice(0, 8), 16) || Math.random(),
            name: e.title,
            type: "experience" as const,
            location: e.locationName,
            photo: "https://images.pexels.com/photos/262978/pexels-photo-262978.jpeg?w=150&h=150&fit=crop",
            status: "pending" as const,
          }));
          setApprovalsList(prev => {
            const existing = new Set(prev.map(a => a.name));
            const newOnes = mapped.filter(a => !existing.has(a.name));
            return [...newOnes, ...prev];
          });
        }
      })
      .catch(() => {});
  }, []);

  useEffect(() => {
    localStorage.setItem("admin_bookings", JSON.stringify(bookingsList));
    localStorage.setItem("admin_reviews", JSON.stringify(reviewsList));
    localStorage.setItem("admin_approvals", JSON.stringify(approvalsList));
    localStorage.setItem("admin_users", JSON.stringify(usersList));
    localStorage.setItem("admin_buddies", JSON.stringify(buddiesList));
    localStorage.setItem("admin_liked_buddies", JSON.stringify(likedBuddies));
  }, [bookingsList, reviewsList, approvalsList, usersList, buddiesList, likedBuddies]);

  const toggleEmailNotifications = () => {
    const newValue = !emailNotifications;
    setEmailNotifications(newValue);
    localStorage.setItem("admin_email_notifications", String(newValue));
    toast.success(newValue ? "Email notifications enabled 🔔" : "Email notifications disabled 🔕");
  };
  const toggleAutoApproveBuddies = () => {
    const newValue = !autoApproveBuddies;
    setAutoApproveBuddies(newValue);
    localStorage.setItem("admin_auto_approve_buddies", String(newValue));
    toast.success(newValue ? "Auto-approve buddies enabled ✅" : "Auto-approve buddies disabled ❌");
  };

  const handleLogout = () => {
    localStorage.removeItem("selectedBuddy");
    localStorage.removeItem("likedBuddies");
    localStorage.removeItem("preferredVibes");
    localStorage.removeItem("comfortLevel");
    localStorage.removeItem("safetyPriority");
    localStorage.removeItem("budgetRange");
    toast.success("Logged out successfully! 👋");
    setTimeout(() => navigate("/auth"), 1000);
  };

  // ========== USER ACTIONS ==========
  const handleViewUser = (name: string) => toast.info(`👁️ Viewing ${name}'s profile`);
  const handleEditUser = (user: any) => {
    setEditingUser({ ...user });
    setShowUserEditModal(true);
  };
  const handleSaveUserEdit = async () => {
    if (authStorage.isAuthenticated()) {
      const apiUserId = editingUser?.email ? apiUserIds.current[editingUser.email.toLowerCase()] : undefined;
      if (apiUserId) {
        try {
          const roleMap: Record<string, string> = { "User": "Traveler", "Buddy": "LocalBuddy", "Admin": "Admin" };
          const backendRole = roleMap[editingUser.role] || editingUser.role;
          await adminApi.assignRole(apiUserId, { role: backendRole });
        } catch { }
      }
    }
    setUsersList(prev => prev.map(u => u.id === editingUser.id ? editingUser : u));
    setShowUserEditModal(false);
    toast.success(`${editingUser.name} updated successfully`);
  };
  const handleBlockUser = async (userId: number, name: string, currentStatus: string) => {
    const newStatus = currentStatus === "active" ? "inactive" : "active";
    if (authStorage.isAuthenticated()) {
      const user = usersList.find(u => u.id === userId);
      const apiUserId = user?.email ? apiUserIds.current[user.email.toLowerCase()] : undefined;
      if (apiUserId) {
        try {
          await adminApi.changeUserStatus(apiUserId, { isActive: newStatus === "active" });
        } catch { }
      }
    }
    setUsersList(prev => prev.map(user => user.id === userId ? { ...user, status: newStatus } : user));
    toast.success(newStatus === "inactive" ? `🚫 ${name} blocked` : `✅ ${name} activated`);
  };
  const handleDeleteUser = (userId: number, name: string) => {
    setUsersList(prev => prev.filter(user => user.id !== userId));
    toast.error(`🗑️ ${name} permanently deleted`);
  };

  // ========== BUDDY ACTIONS ==========
  const handleLikeBuddy = (buddyId: number, buddyName: string) => {
    setLikedBuddies(prev => {
      if (prev.includes(buddyId)) {
        toast.info(`💔 Unliked ${buddyName}`);
        return prev.filter(id => id !== buddyId);
      } else {
        toast.success(`❤️ Liked ${buddyName}`);
        return [...prev, buddyId];
      }
    });
  };
  const handleMessageBuddy = (buddy: any) => {
    localStorage.setItem("selectedBuddy", JSON.stringify({ id: buddy.id, name: buddy.name, photo: buddy.avatar, status: "online" }));
    navigate("/messages", { state: { selectedBuddy: { name: buddy.name, photo: buddy.avatar, status: "online" } } });
  };
  const handleEditBuddy = (buddy: any) => {
    setEditingBuddy({ ...buddy });
    setShowEditModal(true);
  };
  const handleSaveEdit = () => {
    setBuddiesList(prev => prev.map(b => b.id === editingBuddy.id ? editingBuddy : b));
    setShowEditModal(false);
    toast.success(`${editingBuddy.name} updated successfully`);
  };
  const handleAddBuddy = () => {
    const newId = Math.max(...buddiesList.map(b => b.id), 0) + 1;
    const gender = Math.random() > 0.5 ? "men" : "women";
    const randomNum = Math.floor(Math.random() * 50) + 1;
    const avatar = `https://randomuser.me/api/portraits/${gender}/${randomNum}.jpg`;
    const newBuddyObj = {
      id: newId,
      name: newBuddy.name,
      rating: newBuddy.rating,
      tours: newBuddy.tours,
      status: "active",
      earning: newBuddy.earning,
      avatar: avatar,
      location: newBuddy.location,
      phone: newBuddy.phone
    };
    setBuddiesList(prev => [...prev, newBuddyObj]);
    setShowAddModal(false);
    setNewBuddy({ name: "", rating: 4.5, tours: 0, earning: 0, location: "", phone: "" });
    toast.success(`🎉 New buddy ${newBuddy.name} added!`);
  };
  const handleCallBuddy = (phone: string, name: string) => toast.info(`📞 Calling ${name} at ${phone}`);
  const handleViewBuddy = (name: string) => toast.info(`👁️ Viewing ${name}'s profile`);
  const handleDeleteBuddy = (id: number, name: string) => {
    setBuddiesList(prev => prev.filter(b => b.id !== id));
    toast.error(`🗑️ ${name} has been removed`);
  };
  const handleApproveBuddy = async (id: number, name: string) => {
    if (authStorage.isAuthenticated()) {
      const user = usersList.find(u => u.name === name);
      const apiUserId = user?.email ? apiUserIds.current[user.email.toLowerCase()] : undefined;
      if (apiUserId) {
        try {
          await adminApi.updateBuddyVerification(apiUserId, { verificationStatus: "Verified" });
        } catch { }
      }
    }
    setBuddiesList(prev => prev.map(b => b.id === id ? { ...b, status: "active" } : b));
    toast.success(`✅ ${name} approved!`);
  };

  // ========== BOOKING / REVIEW / APPROVAL ==========
  const handleStatusChange = (bookingId: string, newStatus: string) => {
    setBookingsList(prev => prev.map(booking => booking.id === bookingId ? { ...booking, status: newStatus } : booking));
    toast.success(`Booking ${bookingId} marked as ${newStatus}!`);
  };
  const handleViewBooking = (id: string) => toast.info(`Viewing booking ${id} details`);
  const handleRemoveReview = (reviewId: number) => {
    setReviewsList(prev => prev.filter(r => r.id !== reviewId));
    toast.success(`Review has been removed!`);
  };
  const handleViewReview = (target: string) => toast.info(`Reviewing ${target}`);
  const handleApprove = async (id: number, name: string) => {
    if (authStorage.isAuthenticated()) {
      const item = approvalsList.find(a => a.id === id);
      if (item?.type === "experience" && item?.name) {
        try {
          const apiExps = await adminApi.getExperiences({ approvalStatus: "Pending" });
          const match = apiExps.find(e => e.title === item.name);
          if (match) {
            await adminApi.setExperienceApprovalStatus(match.id, { approvalStatus: "Approved" });
          }
        } catch { }
      }
    }
    setApprovalsList(prev => prev.map(a => a.id === id ? { ...a, status: "approved" as ApprovalStatus } : a));
    toast.success(`${name} has been approved! 🎉`);
    setBuddiesList(prev => prev.map(b => b.name === name ? { ...b, status: "active" } : b));
  };
  const handleReject = async (id: number, name: string) => {
    if (authStorage.isAuthenticated()) {
      const item = approvalsList.find(a => a.id === id);
      if (item?.type === "experience" && item?.name) {
        try {
          const apiExps = await adminApi.getExperiences({ approvalStatus: "Pending" });
          const match = apiExps.find(e => e.title === item.name);
          if (match) {
            await adminApi.setExperienceApprovalStatus(match.id, { approvalStatus: "Rejected" });
          }
        } catch { }
      }
    }
    setApprovalsList(prev => prev.map(a => a.id === id ? { ...a, status: "rejected" as ApprovalStatus } : a));
    toast.error(`${name} has been rejected.`);
  };

  const handleAddExperience = () => {
    if (!expForm.name || !expForm.location || expForm.price <= 0) {
      toast.error("Name, location, and price are required");
      return;
    }
    const newExp = {
      id: 100 + Math.floor(Math.random() * 1000),
      name: expForm.name,
      category: expForm.category,
      location: expForm.location,
      price: expForm.price,
      duration: expForm.duration,
      image: expForm.image || "https://images.pexels.com/photos/258117/pexels-photo-258117.jpeg?w=400&h=300&fit=crop",
      description: expForm.description,
      highlights: expForm.highlights.split(",").filter(Boolean).map(s => s.trim()),
    };
    setShowAddExpModal(false);
    setExpForm({ name: "", category: "Historical", location: "", price: 0, duration: "2 hours", description: "", image: "", highlights: "" });
    toast.success(`"${expForm.name}" experience added successfully! 🎉`);
  };

  const handleExport = () => toast.success("Report exported successfully! 📊");
  const handleViewReports = () => setShowReportModal(true);

  const totalUsers = usersList.length;
  const activeBuddies = buddiesList.filter(b => b.status === "active").length;
  const totalBookings = bookingsList.length;
  const totalRevenue = bookingsList.reduce((sum, b) => sum + b.amount, 0);
  const confirmedBookings = bookingsList.filter(b => b.status === "confirmed").length;
  const pendingBookings = bookingsList.filter(b => b.status === "pending").length;

  const filteredUsers = usersList.filter(user => 
    user.name.toLowerCase().includes(userSearchQuery.toLowerCase()) ||
    user.email.toLowerCase().includes(userSearchQuery.toLowerCase())
  );
  const filteredBuddies = buddiesList.filter(buddy => 
    buddy.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    buddy.location.toLowerCase().includes(searchQuery.toLowerCase())
  );
  const filteredBookings = bookingsList.filter(booking => 
    booking.id.toLowerCase().includes(searchQuery.toLowerCase()) ||
    booking.user.toLowerCase().includes(searchQuery.toLowerCase()) ||
    booking.experience.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const getStatusColor = (status: string) => {
    switch(status) {
      case 'active': return 'bg-green-500/20 text-green-400';
      case 'pending': return 'bg-yellow-500/20 text-yellow-400';
      case 'inactive': return 'bg-red-500/20 text-red-400';
      case 'confirmed': return 'bg-green-500/20 text-green-400';
      default: return 'bg-gray-500/20 text-gray-400';
    }
  };

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <div className="flex">
        {/* Sidebar */}
        <aside className="hidden md:block w-64 border-r border-border/50 min-h-[calc(100vh-4rem)] p-4">
          <div className="flex items-center gap-2 mb-6 px-3">
            <Shield className="w-5 h-5 text-accent" />
            <h3 className="text-sm font-bold text-primary">Admin Panel</h3>
          </div>
          <nav className="space-y-1">
            {sidebarItems.map((item) => (
              <button
                key={item}
                onClick={() => {
                  setActiveSidebar(item);
                  setSearchQuery("");
                  setUserSearchQuery("");
                }}
                className={`w-full text-left px-3 py-2 rounded-lg text-sm transition-all duration-200 flex items-center gap-2 ${
                  activeSidebar === item
                    ? "bg-accent text-accent-foreground font-medium shadow-md"
                    : "text-muted-foreground hover:text-foreground hover:bg-secondary"
                }`}
              >
                {item === "Overview" && <Activity className="w-4 h-4" />}
                {item === "Users" && <Users className="w-4 h-4" />}
                {item === "Buddies" && <UserCheck className="w-4 h-4" />}
                {item === "Bookings" && <Calendar className="w-4 h-4" />}
                {item === "Reviews" && <Star className="w-4 h-4" />}
                {item === "Revenue" && <DollarSign className="w-4 h-4" />}
                {item === "Reports" && <FileText className="w-4 h-4" />}
                {item === "Settings" && <Settings className="w-4 h-4" />}
                {item}
              </button>
            ))}
          </nav>
          <div className="mt-8 pt-4 border-t border-border">
            <button onClick={handleLogout} className="w-full text-left px-3 py-2 rounded-lg text-sm text-red-500 hover:bg-red-500/10 flex items-center gap-2">
              <LogOut className="w-4 h-4" /> Logout
            </button>
          </div>
        </aside>

        {/* Main Content */}
        <main className="flex-1 p-6">
          <div className="flex items-center justify-between mb-6 flex-wrap gap-3">
            <div>
              <h1 className="text-xl font-bold text-gradient-purple">{activeSidebar}</h1>
              <p className="text-xs text-muted-foreground">Manage {activeSidebar.toLowerCase()}</p>
            </div>
            <div className="flex gap-2">
              <button onClick={() => setShowAddExpModal(true)} className="flex items-center gap-1.5 px-3 py-2 bg-accent rounded-lg text-xs">
                <Sparkles size={14} /> Add Experience
              </button>
              <button onClick={handleExport} className="flex items-center gap-1.5 px-3 py-2 border border-border rounded-lg text-xs hover:bg-secondary">
                <Download size={14} /> Export
              </button>
              <button onClick={handleViewReports} className="flex items-center gap-1.5 px-3 py-2 bg-primary rounded-lg text-xs">
                <FileText size={14} /> View Reports
              </button>
            </div>
          </div>

          {/* BUDDIES PAGE */}
          {activeSidebar === "Buddies" && (
            <div className="card-glass p-5">
              <div className="flex justify-between items-center mb-4">
                <h2 className="font-bold text-lg">All Buddies</h2>
                <div className="flex gap-2">
                  <div className="flex items-center gap-2 bg-secondary px-3 py-1.5 rounded-lg">
                    <Search className="w-4 h-4" />
                    <input type="text" placeholder="Search buddies..." className="bg-transparent text-sm" value={searchQuery} onChange={e => setSearchQuery(e.target.value)} />
                  </div>
                  <button onClick={() => toast.info("Filter options")} className="p-1.5 bg-secondary rounded"><Filter className="w-4 h-4" /></button>
                  <button onClick={() => setShowAddModal(true)} className="flex items-center gap-1 px-3 py-1.5 bg-accent rounded text-sm"><Plus className="w-4 h-4" /> Add Buddy</button>
                </div>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b"><th>Name</th><th>Rating</th><th>Tours</th><th>Earnings</th><th>Location</th><th>Status</th><th className="text-center">Actions</th></tr></thead>
                  <tbody>
                    {filteredBuddies.map(buddy => (
                      <tr key={buddy.id} className="border-b hover:bg-secondary/30">
                        <td className="py-3"><div className="flex items-center gap-2"><img src={buddy.avatar} className="w-8 h-8 rounded-full" />{buddy.name}</div></td>
                        <td className="py-3"><Star className="inline fill-yellow-500 text-yellow-500 w-3.5 h-3.5" /> {buddy.rating}</td>
                        <td className="py-3">{buddy.tours}</td>
                        <td className="py-3">${buddy.earning}</td>
                        <td className="py-3"><MapPin className="inline w-3 h-3" /> {buddy.location}</td>
                        <td className="py-3"><span className={`px-2 py-0.5 rounded-full text-xs ${getStatusColor(buddy.status)}`}>{buddy.status}</span></td>
                        <td className="py-3 text-center">
                          <div className="flex justify-center gap-1">
                            <button onClick={() => handleViewBuddy(buddy.name)} title="View"><Eye className="w-4 h-4 text-accent" /></button>
                            <button onClick={() => handleMessageBuddy(buddy)} title="Message"><MessageSquare className="w-4 h-4 text-blue-500" /></button>
                            <button onClick={() => handleCallBuddy(buddy.phone, buddy.name)} title="Call"><Phone className="w-4 h-4 text-green-500" /></button>
                            <button onClick={() => handleLikeBuddy(buddy.id, buddy.name)} title="Like"><Heart className={`w-4 h-4 ${likedBuddies.includes(buddy.id) ? "fill-pink-500 text-pink-500" : "text-pink-500"}`} /></button>
                            <button onClick={() => handleEditBuddy(buddy)} title="Edit"><Edit className="w-4 h-4 text-yellow-500" /></button>
                            {buddy.status === "pending" && <button onClick={() => handleApproveBuddy(buddy.id, buddy.name)} title="Approve"><CheckCircle className="w-4 h-4 text-green-500" /></button>}
                            <button onClick={() => handleDeleteBuddy(buddy.id, buddy.name)} title="Delete"><Trash2 className="w-4 h-4 text-red-500" /></button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* ADD BUDDY MODAL */}
          {showAddModal && (
            <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4">
              <div className="bg-background border border-border rounded-2xl p-6 max-w-md w-full">
                <div className="flex justify-between items-center mb-4"><h2 className="text-xl font-bold">Add New Buddy</h2><button onClick={() => setShowAddModal(false)}><X className="w-5 h-5" /></button></div>
                <div className="space-y-3">
                  <input className="w-full p-2 bg-secondary rounded" placeholder="Name" value={newBuddy.name} onChange={e => setNewBuddy({...newBuddy, name: e.target.value})} />
                  <input className="w-full p-2 bg-secondary rounded" type="number" step="0.1" placeholder="Rating" value={newBuddy.rating} onChange={e => setNewBuddy({...newBuddy, rating: parseFloat(e.target.value)})} />
                  <input className="w-full p-2 bg-secondary rounded" type="number" placeholder="Tours" value={newBuddy.tours} onChange={e => setNewBuddy({...newBuddy, tours: parseInt(e.target.value)})} />
                  <input className="w-full p-2 bg-secondary rounded" type="number" placeholder="Earnings ($)" value={newBuddy.earning} onChange={e => setNewBuddy({...newBuddy, earning: parseInt(e.target.value)})} />
                  <input className="w-full p-2 bg-secondary rounded" placeholder="Location" value={newBuddy.location} onChange={e => setNewBuddy({...newBuddy, location: e.target.value})} />
                  <input className="w-full p-2 bg-secondary rounded" placeholder="Phone" value={newBuddy.phone} onChange={e => setNewBuddy({...newBuddy, phone: e.target.value})} />
                </div>
                <div className="flex gap-3 mt-6"><button onClick={handleAddBuddy} className="flex-1 bg-accent py-2 rounded" disabled={!newBuddy.name}>Add Buddy</button><button onClick={() => setShowAddModal(false)} className="px-4 py-2 border rounded">Cancel</button></div>
              </div>
            </div>
          )}

          {/* EDIT BUDDY MODAL */}
          {showEditModal && editingBuddy && (
            <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4">
              <div className="bg-background border border-border rounded-2xl p-6 max-w-md w-full">
                <div className="flex justify-between items-center mb-4"><h2 className="text-xl font-bold">Edit Buddy</h2><button onClick={() => setShowEditModal(false)}><X className="w-5 h-5" /></button></div>
                <div className="space-y-3">
                  <input className="w-full p-2 bg-secondary rounded" value={editingBuddy.name} onChange={e => setEditingBuddy({...editingBuddy, name: e.target.value})} placeholder="Name" />
                  <input className="w-full p-2 bg-secondary rounded" type="number" step="0.1" value={editingBuddy.rating} onChange={e => setEditingBuddy({...editingBuddy, rating: parseFloat(e.target.value)})} placeholder="Rating" />
                  <input className="w-full p-2 bg-secondary rounded" type="number" value={editingBuddy.tours} onChange={e => setEditingBuddy({...editingBuddy, tours: parseInt(e.target.value)})} placeholder="Tours" />
                  <input className="w-full p-2 bg-secondary rounded" type="number" value={editingBuddy.earning} onChange={e => setEditingBuddy({...editingBuddy, earning: parseInt(e.target.value)})} placeholder="Earnings" />
                  <input className="w-full p-2 bg-secondary rounded" value={editingBuddy.location} onChange={e => setEditingBuddy({...editingBuddy, location: e.target.value})} placeholder="Location" />
                  <input className="w-full p-2 bg-secondary rounded" value={editingBuddy.phone} onChange={e => setEditingBuddy({...editingBuddy, phone: e.target.value})} placeholder="Phone" />
                </div>
                <div className="flex gap-3 mt-6"><button onClick={handleSaveEdit} className="flex-1 bg-accent py-2 rounded">Save</button><button onClick={() => setShowEditModal(false)} className="px-4 py-2 border rounded">Cancel</button></div>
              </div>
            </div>
          )}

          {/* USERS PAGE */}
          {activeSidebar === "Users" && (
            <div className="card-glass p-5">
              <div className="flex justify-between items-center mb-4">
                <h2 className="font-bold text-lg">All Users</h2>
                <div className="flex gap-2">
                  <div className="flex items-center gap-2 bg-secondary px-3 py-1.5 rounded-lg">
                    <Search className="w-4 h-4" />
                    <input type="text" placeholder="Search by name or email..." className="bg-transparent text-sm" value={userSearchQuery} onChange={e => setUserSearchQuery(e.target.value)} />
                  </div>
                </div>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b"><th>User</th><th>Email</th><th>Role</th><th>Status</th><th>Joined</th><th className="text-center">Actions</th></tr></thead>
                  <tbody>
                    {filteredUsers.map(user => (
                      <tr key={user.id} className="border-b hover:bg-secondary/30">
                        <td className="py-3"><div className="flex items-center gap-2"><img src={user.avatar} className="w-8 h-8 rounded-full" />{user.name}</div></td>
                        <td className="py-3">{user.email}</td>
                        <td className="py-3"><span className={`px-2 py-0.5 rounded-full text-xs ${user.role === 'Buddy' ? 'bg-accent/20 text-accent' : 'bg-primary/20 text-primary'}`}>{user.role}</span></td>
                        <td className="py-3"><span className={`px-2 py-0.5 rounded-full text-xs ${getStatusColor(user.status)}`}>{user.status}</span></td>
                        <td className="py-3">{user.joined}</td>
                        <td className="py-3 text-center">
                          <div className="flex justify-center gap-2">
                            <button onClick={() => handleViewUser(user.name)} title="View"><Eye className="w-4 h-4 text-accent" /></button>
                            <button onClick={() => handleEditUser(user)} title="Edit"><Edit className="w-4 h-4 text-yellow-500" /></button>
                            {user.status === 'active' ? (
                              <button onClick={() => handleBlockUser(user.id, user.name, user.status)} title="Block"><Ban className="w-4 h-4 text-red-500" /></button>
                            ) : (
                              <button onClick={() => handleBlockUser(user.id, user.name, user.status)} title="Activate"><Check className="w-4 h-4 text-green-500" /></button>
                            )}
                            <button onClick={() => handleDeleteUser(user.id, user.name)} title="Delete"><Trash2 className="w-4 h-4 text-red-500" /></button>
                          </div>
                        </td>
                      </tr>
                    ))}
                    {filteredUsers.length === 0 && (
                      <tr><td colSpan={6} className="text-center py-8 text-muted-foreground">No users found</td></tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* USER EDIT MODAL */}
          {showUserEditModal && editingUser && (
            <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4">
              <div className="bg-background border border-border rounded-2xl p-6 max-w-md w-full">
                <div className="flex justify-between items-center mb-4">
                  <h2 className="text-xl font-bold">Edit User</h2>
                  <button onClick={() => setShowUserEditModal(false)} className="p-1 hover:bg-secondary rounded">
                    <X className="w-5 h-5" />
                  </button>
                </div>
                <div className="space-y-3">
                  <input className="w-full p-2 bg-secondary rounded" value={editingUser.name} onChange={e => setEditingUser({...editingUser, name: e.target.value})} placeholder="Name" />
                  <input className="w-full p-2 bg-secondary rounded" value={editingUser.email} onChange={e => setEditingUser({...editingUser, email: e.target.value})} placeholder="Email" />
                  <select className="w-full p-2 bg-secondary rounded" value={editingUser.role} onChange={e => setEditingUser({...editingUser, role: e.target.value})}>
                    <option value="User">User</option>
                    <option value="Buddy">Buddy</option>
                    <option value="Admin">Admin</option>
                  </select>
                  <select className="w-full p-2 bg-secondary rounded" value={editingUser.status} onChange={e => setEditingUser({...editingUser, status: e.target.value})}>
                    <option value="active">Active</option>
                    <option value="inactive">Inactive</option>
                  </select>
                </div>
                <div className="flex gap-3 mt-6">
                  <button onClick={handleSaveUserEdit} className="flex-1 bg-accent py-2 rounded">Save Changes</button>
                  <button onClick={() => setShowUserEditModal(false)} className="px-4 py-2 border border-border rounded">Cancel</button>
                </div>
              </div>
            </div>
          )}

          {/* BOOKINGS PAGE */}
          {activeSidebar === "Bookings" && (
            <div className="card-glass p-5">
              <div className="flex items-center justify-between mb-4">
                <h2 className="font-bold text-lg">All Bookings</h2>
                <div className="flex gap-2">
                  <div className="flex items-center gap-2 bg-secondary px-3 py-1.5 rounded-lg">
                    <Search className="w-4 h-4" />
                    <input type="text" placeholder="Search bookings..." className="bg-transparent text-sm" value={searchQuery} onChange={e => setSearchQuery(e.target.value)} />
                  </div>
                </div>
              </div>
              <div className="grid grid-cols-3 gap-4 mb-4">
                <div className="p-3 bg-secondary/30 rounded text-center"><p className="text-xs text-muted-foreground">Total</p><p className="text-xl font-bold">{totalBookings}</p></div>
                <div className="p-3 bg-secondary/30 rounded text-center"><p className="text-xs text-muted-foreground">Confirmed</p><p className="text-xl font-bold text-green-400">{confirmedBookings}</p></div>
                <div className="p-3 bg-secondary/30 rounded text-center"><p className="text-xs text-muted-foreground">Pending</p><p className="text-xl font-bold text-yellow-400">{pendingBookings}</p></div>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b"><th>ID</th><th>User</th><th>Experience</th><th>Status</th><th>Date</th><th>Amount</th><th className="text-center">Actions</th></tr></thead>
                  <tbody>
                    {filteredBookings.length === 0 ? (
                      <tr><td colSpan={7} className="text-center py-8 text-muted-foreground">No bookings found</td></tr>
                    ) : (
                      filteredBookings.map(b => (
                        <tr key={b.id} className="border-b hover:bg-secondary/30">
                          <td className="py-2 text-xs">{b.id}</td>
                          <td className="py-2">{b.user}</td>
                          <td className="py-2">{b.experience}</td>
                          <td className="py-2"><span className={`px-2 py-0.5 rounded-full text-xs ${getStatusColor(b.status)}`}>{b.status}</span></td>
                          <td className="py-2 text-xs">{b.date}</td>
                          <td className="py-2 font-medium">${b.amount}</td>
                          <td className="py-2 text-center">
                            <div className="flex justify-center gap-1">
                              <button onClick={() => handleViewBooking(b.id)} className="text-xs bg-accent/20 text-accent px-2 py-1 rounded" title="View">👁</button>
                              {b.status === 'confirmed' ? (
                                <button onClick={() => handleStatusChange(b.id, 'pending')} className="text-xs bg-yellow-500/20 text-yellow-400 px-2 py-1 rounded">↻ Pending</button>
                              ) : b.status === 'pending' ? (
                                <button onClick={() => handleStatusChange(b.id, 'confirmed')} className="text-xs bg-green-500/20 text-green-400 px-2 py-1 rounded">✓ Confirm</button>
                              ) : null}
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* REVIEWS PAGE */}
          {activeSidebar === "Reviews" && (
            <div className="card-glass p-5">
              <h2 className="font-bold text-lg mb-4">Flagged Reviews</h2>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b"><th>Reviewer</th><th>Target</th><th>Reason</th><th>Date</th><th>Actions</th></tr></thead>
                  <tbody>
                    {reviewsList.map(r => (
                      <tr key={r.id} className="border-b">
                        <td className="py-2">{r.reviewer}</td>
                        <td className="py-2">{r.target}</td>
                        <td className="py-2"><span className="bg-red-500/20 text-red-400 px-2 py-0.5 rounded text-xs">{r.reason}</span></td>
                        <td className="py-2">{r.date}</td>
                        <td className="py-2"><button onClick={() => handleRemoveReview(r.id)} className="bg-red-500/20 text-red-400 px-2 py-1 rounded text-xs">Remove</button></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* REVENUE PAGE */}
          {activeSidebar === "Revenue" && (
            <div className="card-glass p-6 text-center">
              <p className="text-3xl font-bold text-accent">${totalRevenue}</p>
              <p className="text-xs text-green-500 mt-1">↑ +15% from last month</p>
              <div className="grid grid-cols-2 gap-4 mt-4">
                <div className="p-3 bg-secondary/30 rounded"><p className="text-xs">Confirmed</p><p className="text-xl font-bold">{confirmedBookings}</p></div>
                <div className="p-3 bg-secondary/30 rounded"><p className="text-xs">Pending</p><p className="text-xl font-bold">{pendingBookings}</p></div>
              </div>
            </div>
          )}

          {/* REPORTS PAGE */}
          {activeSidebar === "Reports" && (
            <div className="card-glass p-6">
              <h2 className="font-bold text-lg mb-4">Reports</h2>
              <div className="space-y-3">
                {["User Activity", "Revenue", "Bookings"].map(r => (
                  <div key={r} className="flex justify-between items-center p-3 bg-secondary/30 rounded cursor-pointer" onClick={handleExport}>
                    <span>{r} Report</span>
                    <Download size={16} />
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* SETTINGS PAGE */}
          {activeSidebar === "Settings" && (
            <div className="card-glass p-6">
              <h2 className="font-bold text-lg mb-4">Settings</h2>
              <div className="space-y-3">
                <div className="flex justify-between items-center p-3 bg-secondary/30 rounded">
                  <span>Email Notifications</span>
                  <button onClick={toggleEmailNotifications} className="px-3 py-1 bg-accent rounded text-sm">{emailNotifications ? "Disable" : "Enable"}</button>
                </div>
                <div className="flex justify-between items-center p-3 bg-secondary/30 rounded">
                  <span>Auto-approve Buddies</span>
                  <button onClick={toggleAutoApproveBuddies} className="px-3 py-1 bg-accent rounded text-sm">{autoApproveBuddies ? "Disable" : "Enable"}</button>
                </div>
              </div>
            </div>
          )}

          {/* OVERVIEW PAGE */}
          {activeSidebar === "Overview" && (
            <>
              <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
                {[
                  { icon: Users, value: totalUsers, label: "Total Users" },
                  { icon: UserCheck, value: activeBuddies, label: "Active Buddies" },
                  { icon: Calendar, value: totalBookings, label: "Total Bookings" },
                  { icon: DollarSign, value: `$${totalRevenue}`, label: "Revenue" },
                ].map(s => (
                  <div key={s.label} className="card-glass p-4">
                    <div className="w-9 h-9 rounded-lg bg-accent/15 flex items-center justify-center mb-2"><s.icon className="w-4 h-4 text-accent" /></div>
                    <p className="text-xl font-extrabold">{s.value}</p>
                    <p className="text-xs text-muted-foreground">{s.label}</p>
                  </div>
                ))}
              </div>
              <div className="card-glass p-5">
                <h2 className="font-bold mb-3">
                Pending Approvals
                <span className="ml-2 text-xs font-normal text-muted-foreground">
                  ({approvalsList.filter(a => a.status === "pending").length} pending)
                </span>
              </h2>
                {approvalsList.filter(a => a.status === "pending").length === 0 ? (
                  <div className="text-center py-6 text-sm text-muted-foreground">
                    <CheckCircle className="w-8 h-8 mx-auto mb-2 text-green-500 opacity-60" />
                    All approvals resolved! ✨
                  </div>
                ) : (
                  approvalsList.filter(a => a.status === "pending").map(a => (
                    <div key={a.id} className="flex justify-between items-center p-2 border-b">
                      <div className="flex items-center gap-2"><img src={a.photo} className="w-8 h-8 rounded-full object-cover" />{a.name} <span className="text-xs text-muted-foreground">({a.type} · {a.location})</span></div>
                      <div><button onClick={() => handleApprove(a.id, a.name)} className="text-green-500 hover:text-green-400 mr-2 text-sm">✓ Approve</button><button onClick={() => handleReject(a.id, a.name)} className="text-red-500 hover:text-red-400 text-sm">✕ Reject</button></div>
                    </div>
                  ))
                )}
                {approvalsList.filter(a => a.status !== "pending").length > 0 && (
                  <div className="mt-3 pt-3 border-t border-border">
                    <p className="text-xs text-muted-foreground mb-2">History:</p>
                    {approvalsList.filter(a => a.status !== "pending").map(a => (
                      <div key={a.id} className="flex items-center gap-2 text-xs py-1">
                        <span className={`w-2 h-2 rounded-full ${a.status === "approved" ? "bg-green-500" : "bg-red-500"}`} />
                        <span>{a.name}</span>
                        <span className={a.status === "approved" ? "text-green-400" : "text-red-400"}>
                          {a.status === "approved" ? "Approved" : "Rejected"}
                        </span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </>
          )}
        </main>
      </div>

      {/* VIEW REPORTS MODAL (FIXED) */}
      {showAddExpModal && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-background border border-border rounded-2xl p-6 max-w-lg w-full max-h-[90vh] overflow-y-auto">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-xl font-bold flex items-center gap-2"><Sparkles className="w-5 h-5 text-accent" /> Add New Experience</h2>
              <button onClick={() => setShowAddExpModal(false)} className="p-1 hover:bg-secondary rounded"><X className="w-5 h-5" /></button>
            </div>
            <div className="space-y-3">
              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Experience Name *</label>
                <input className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm" value={expForm.name} onChange={e => setExpForm({...expForm, name: e.target.value})} placeholder="e.g. Sunset Nile Cruise" />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Category</label>
                  <select className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm" value={expForm.category} onChange={e => setExpForm({...expForm, category: e.target.value})}>
                    <option>Cruise</option><option>Historical</option><option>Adventure</option><option>Cultural</option><option>Water Sports</option><option>Food</option>
                  </select>
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Duration</label>
                  <select className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm" value={expForm.duration} onChange={e => setExpForm({...expForm, duration: e.target.value})}>
                    <option>1 hour</option><option>2 hours</option><option>3 hours</option><option>4 hours</option><option>5 hours</option><option>6 hours</option><option>2 days</option>
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Location *</label>
                  <input className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm" value={expForm.location} onChange={e => setExpForm({...expForm, location: e.target.value})} placeholder="e.g. Cairo" />
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Price ($) *</label>
                  <input type="number" min={0} className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm" value={expForm.price || ""} onChange={e => setExpForm({...expForm, price: parseFloat(e.target.value) || 0})} />
                </div>
              </div>
              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Description</label>
                <textarea rows={2} className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm" value={expForm.description} onChange={e => setExpForm({...expForm, description: e.target.value})} placeholder="Describe the experience..." />
              </div>
              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Highlights (comma-separated)</label>
                <input className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm" value={expForm.highlights} onChange={e => setExpForm({...expForm, highlights: e.target.value})} placeholder="e.g. Expert guide, Lunch included, Photo stops" />
              </div>
              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Image URL (optional)</label>
                <input className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm" value={expForm.image} onChange={e => setExpForm({...expForm, image: e.target.value})} placeholder="https://..." />
              </div>
            </div>
            <div className="flex gap-3 mt-6">
              <button onClick={handleAddExperience} className="flex-1 bg-accent text-accent-foreground py-2.5 rounded-xl font-medium hover:opacity-90">Add Experience</button>
              <button onClick={() => setShowAddExpModal(false)} className="px-4 py-2.5 border border-border rounded-xl hover:bg-secondary">Cancel</button>
            </div>
          </div>
        </div>
      )}

      {showReportModal && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-background border border-border rounded-2xl p-6 max-w-2xl w-full">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-xl font-bold">Platform Report</h2>
              <button onClick={() => setShowReportModal(false)} className="p-1 hover:bg-secondary rounded">
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
              <div className="p-3 bg-secondary/30 rounded text-center">
                <Users className="w-8 h-8 mx-auto mb-1" />
                <p className="text-2xl font-bold">{totalUsers}</p>
                <p className="text-xs">Total Users</p>
              </div>
              <div className="p-3 bg-secondary/30 rounded text-center">
                <UserCheck className="w-8 h-8 mx-auto mb-1" />
                <p className="text-2xl font-bold">{activeBuddies}</p>
                <p className="text-xs">Active Buddies</p>
              </div>
              <div className="p-3 bg-secondary/30 rounded text-center">
                <Calendar className="w-8 h-8 mx-auto mb-1" />
                <p className="text-2xl font-bold">{totalBookings}</p>
                <p className="text-xs">Total Bookings</p>
              </div>
              <div className="p-3 bg-secondary/30 rounded text-center">
                <DollarSign className="w-8 h-8 mx-auto mb-1" />
                <p className="text-2xl font-bold">${totalRevenue}</p>
                <p className="text-xs">Revenue</p>
              </div>
            </div>
            <div className="border-t pt-4">
              <p className="font-medium mb-2">Booking breakdown</p>
              <div className="flex justify-between">
                <span>Confirmed: <strong className="text-green-400">{confirmedBookings}</strong></span>
                <span>Pending: <strong className="text-yellow-400">{pendingBookings}</strong></span>
              </div>
            </div>
            <div className="flex gap-3 mt-6">
              <button onClick={() => toast.success("Downloaded (demo)")} className="flex-1 bg-accent py-2 rounded">Download Report</button>
              <button onClick={() => setShowReportModal(false)} className="px-4 py-2 border border-border rounded">Close</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Admin;