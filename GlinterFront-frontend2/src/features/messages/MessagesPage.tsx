import { useState, useEffect } from "react";
import { Bell, MessageSquare, Plane, CheckCircle, Gift, Star, AlertTriangle, ArrowLeft, Send, CheckCheck, X, ExternalLink, Sparkles } from "lucide-react";
import { useLocation, useNavigate } from "react-router-dom";
import Navbar from "@/components/Navbar";
import { toast } from "sonner";

const notifications = [
  { 
    icon: Plane, 
    color: "bg-primary/20 text-primary", 
    title: "Trip to Alexandria confirmed!", 
    desc: "Your 3-day trip starting Dec 20 is all set", 
    time: "2 hours ago", 
    unread: true, 
    buddyName: null,
    type: "trip",
    link: "/trips"
  },
  { 
    icon: CheckCircle, 
    color: "bg-green-500/20 text-green-400", 
    title: "Ahmed accepted your request", 
    desc: "You can now chat with your local buddy in Luxor", 
    time: "5 hours ago", 
    unread: true, 
    buddyName: "Ahmed Hassan",
    type: "message",
    link: "/messages"
  },
  { 
    icon: Gift, 
    color: "bg-accent/20 text-accent", 
    title: "Special Offer: 20% off Nile Cruise", 
    desc: "Limited time offer for premium members", 
    time: "1 day ago", 
    unread: false, 
    buddyName: null,
    type: "offer",
    link: "/offers"
  },
  { 
    icon: Star, 
    color: "bg-gold/20 text-gold", 
    title: "Rate your experience with Sara", 
    desc: "How was your food tour in Cairo?", 
    time: "2 days ago", 
    unread: false, 
    buddyName: "Sara Mohamed",
    type: "rating",
    link: "/rate"
  },
  { 
    icon: AlertTriangle, 
    color: "bg-destructive/20 text-destructive", 
    title: "Weather Alert: Heavy rain expected", 
    desc: "Plan accordingly for your trip to Aswan", 
    time: "3 days ago", 
    unread: false, 
    buddyName: null,
    type: "alert",
    link: "/weather"
  },
];

interface Message {
  id: number;
  text: string;
  sender: "user" | "buddy";
  timestamp: Date;
}

interface Conversation {
  buddyId: string;
  buddyName: string;
  buddyPhoto: string;
  lastMessage: string;
  lastMessageTime: Date;
  unreadCount: number;
  messages: Message[];
  isFirstTime?: boolean;
}

const Messages = () => {
  const [activeTab, setActiveTab] = useState<"notifications" | "messages">("notifications");
  const [selectedBuddy, setSelectedBuddy] = useState<any>(null);
  const [messageInput, setMessageInput] = useState("");
  const [notificationsList, setNotificationsList] = useState(notifications);
  const [showChat, setShowChat] = useState(false);
  const [selectedNotification, setSelectedNotification] = useState<any>(null);
  const [showNotificationModal, setShowNotificationModal] = useState(false);
  const [showWelcome, setShowWelcome] = useState(false);
  
  const [conversations, setConversations] = useState<Conversation[]>([
    {
      buddyId: "Ahmed Hassan",
      buddyName: "Ahmed Hassan",
      buddyPhoto: "https://randomuser.me/api/portraits/men/1.jpg",
      lastMessage: "Hey! I'd love to show you around Luxor!",
      lastMessageTime: new Date(Date.now() - 3600000),
      unreadCount: 2,
      isFirstTime: false,
      messages: [
        { id: 1, text: "Hi Ahmed! I'm planning to visit Luxor next week", sender: "user", timestamp: new Date(Date.now() - 7200000) },
        { id: 2, text: "That's great! I'd love to show you around Luxor!", sender: "buddy", timestamp: new Date(Date.now() - 3600000) },
        { id: 3, text: "Can you recommend some good places to visit?", sender: "user", timestamp: new Date(Date.now() - 1800000) },
      ]
    },
    {
      buddyId: "Sara Mohamed",
      buddyName: "Sara Mohamed",
      buddyPhoto: "https://randomuser.me/api/portraits/women/2.jpg",
      lastMessage: "The food tour was amazing! 🍽️",
      lastMessageTime: new Date(Date.now() - 86400000),
      unreadCount: 0,
      isFirstTime: false,
      messages: [
        { id: 1, text: "Hi Sara! I heard you do food tours in Cairo", sender: "user", timestamp: new Date(Date.now() - 172800000) },
        { id: 2, text: "Yes! I'd love to take you to the best local spots", sender: "buddy", timestamp: new Date(Date.now() - 86400000) },
        { id: 3, text: "The food tour was amazing! 🍽️", sender: "buddy", timestamp: new Date(Date.now() - 86400000) },
      ]
    }
  ]);

  const location = useLocation();
  const navigate = useNavigate();

  const suggestedMessages = [
    "Hi! I'm excited to explore Egypt with you! 🎉",
    "Can you tell me more about your tours?",
    "What are your favorite places to visit?",
    "Do you have any recommendations for food?",
    "I'd love to learn about the local culture!",
    "What's the best time to visit?",
  ];

  useEffect(() => {
    const buddyFromState = location.state?.selectedBuddy;
    const buddyFromStorage = localStorage.getItem("selectedBuddy");
    
    let buddyToAdd = buddyFromState;
    if (!buddyToAdd && buddyFromStorage) {
      buddyToAdd = JSON.parse(buddyFromStorage);
      localStorage.removeItem("selectedBuddy");
    }
    
    if (buddyToAdd) {
      const existingConv = conversations.find(conv => conv.buddyId === buddyToAdd.name);
      
      if (!existingConv) {
        const newConversation: Conversation = {
          buddyId: buddyToAdd.name,
          buddyName: buddyToAdd.name,
          buddyPhoto: buddyToAdd.photo,
          lastMessage: "Start your first conversation!",
          lastMessageTime: new Date(),
          unreadCount: 0,
          isFirstTime: true,
          messages: []
        };
        setConversations([newConversation, ...conversations]);
        setSelectedBuddy(newConversation);
        setShowChat(true);
        setShowWelcome(true);
        toast.success(`New conversation started with ${buddyToAdd.name}! Say hi! 👋`);
      } else {
        setSelectedBuddy(existingConv);
        setShowChat(true);
        setShowWelcome(false);
        toast.success(`Continue chatting with ${buddyToAdd.name}!`);
      }
    }
  }, [location.state]);

  const markAllAsRead = () => {
    const updatedNotifications = notificationsList.map(notification => ({
      ...notification,
      unread: false
    }));
    setNotificationsList(updatedNotifications);
    toast.success("All notifications marked as read! ✨");
  };

  const openNotification = (notification: any, index: number) => {
    const updatedNotifications = [...notificationsList];
    updatedNotifications[index].unread = false;
    setNotificationsList(updatedNotifications);
    
    setSelectedNotification(notification);
    setShowNotificationModal(true);
  };

  const handleNotificationAction = () => {
    if (!selectedNotification) return;
    
    setShowNotificationModal(false);
    
    if (selectedNotification.type === "message" && selectedNotification.buddyName) {
      const conversation = conversations.find(conv => conv.buddyName === selectedNotification.buddyName);
      if (conversation) {
        setSelectedBuddy(conversation);
        setShowChat(true);
        setActiveTab("messages");
        setShowWelcome(false);
      } else {
        navigate("/local-buddies");
        toast.info(`Connect with ${selectedNotification.buddyName} to start chatting`);
      }
    } else {
      toast.success("Notification marked as read");
    }
  };

  const handleSendMessage = () => {
    if (!messageInput.trim() || !selectedBuddy) return;
    
    const newMessage: Message = {
      id: selectedBuddy.messages.length + 1,
      text: messageInput,
      sender: "user",
      timestamp: new Date()
    };
    
    const updatedConversations = conversations.map(conv => {
      if (conv.buddyId === selectedBuddy.buddyId) {
        return {
          ...conv,
          messages: [...conv.messages, newMessage],
          lastMessage: messageInput,
          lastMessageTime: new Date(),
          unreadCount: 0,
          isFirstTime: false
        };
      }
      return conv;
    });
    
    setConversations(updatedConversations);
    setSelectedBuddy({
      ...selectedBuddy,
      messages: [...selectedBuddy.messages, newMessage],
      lastMessage: messageInput,
      lastMessageTime: new Date(),
      isFirstTime: false
    });
    setMessageInput("");
    setShowWelcome(false);
    
    toast.loading(`${selectedBuddy.buddyName} is typing...`, { duration: 1500 });
    
    setTimeout(() => {
      const replyMessage: Message = {
        id: selectedBuddy.messages.length + 2,
        text: getAutoReply(selectedBuddy.buddyName, messageInput),
        sender: "buddy",
        timestamp: new Date()
      };
      
      const convWithReply = conversations.map(conv => {
        if (conv.buddyId === selectedBuddy.buddyId) {
          return {
            ...conv,
            messages: [...conv.messages, newMessage, replyMessage],
            lastMessage: replyMessage.text,
            lastMessageTime: new Date(),
            unreadCount: 0,
            isFirstTime: false
          };
        }
        return conv;
      });
      
      setConversations(convWithReply);
      setSelectedBuddy((prev: any) => ({
        ...prev,
        messages: [...prev.messages, replyMessage],
        lastMessage: replyMessage.text,
        lastMessageTime: new Date(),
        isFirstTime: false
      }));
      toast.dismiss();
      toast.success("New message received!");
    }, 2000);
  };
  
  const getAutoReply = (buddyName: string, userMessage: string) => {
    const firstTimeReplies = [
      `Welcome! 🎉 I'm so excited to connect with you. How can I help make your trip amazing?`,
      `Hey there! 👋 Thanks for reaching out. What brings you to ${buddyName.split(" ")[0]}'s city?`,
      `Hello! 🌟 I'd love to show you around. What kind of experiences are you looking for?`,
      `Hi friend! 😊 Ready for an adventure? Let me know your interests and I'll plan something special!`,
    ];
    
    const regularReplies = [
      `Thanks for your message! I'm excited to help you explore Egypt. 😊`,
      `That sounds interesting! Let me check the best spots for you.`,
      `I know a great place! Would you like me to arrange something?`,
      `Absolutely! When would you like to meet up?`,
      `Thanks for reaching out! I'll make sure you have an amazing experience. 🌟`,
      `Great question! Let me share some local insights with you.`,
      `I love talking about this! Here's what I recommend...`,
    ];
    
    const isFirstMessage = selectedBuddy?.messages?.length === 0;
    
    if (isFirstMessage) {
      return firstTimeReplies[Math.floor(Math.random() * firstTimeReplies.length)];
    }
    
    return regularReplies[Math.floor(Math.random() * regularReplies.length)];
  };

  const handleSuggestedMessage = (message: string) => {
    setMessageInput(message);
    setTimeout(() => {
      handleSendMessage();
    }, 100);
  };

  const getConversationList = () => {
    return [...conversations].sort((a, b) => 
      b.lastMessageTime.getTime() - a.lastMessageTime.getTime()
    );
  };

  const formatTime = (date: Date) => {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const hours = diff / (1000 * 60 * 60);
    
    if (hours < 1) return `${Math.floor(hours * 60)} min ago`;
    if (hours < 24) return `${Math.floor(hours)} hours ago`;
    return `${Math.floor(hours / 24)} days ago`;
  };

  const getUnreadNotificationsCount = () => {
    return notificationsList.filter(n => n.unread).length;
  };

  const openConversation = (conv: Conversation) => {
    setSelectedBuddy(conv);
    setShowChat(true);
    setShowWelcome(conv.isFirstTime && conv.messages.length === 0);
  };

  const closeChat = () => {
    setShowChat(false);
    setSelectedBuddy(null);
    setShowWelcome(false);
  };

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <div className="container mx-auto px-4 py-8 max-w-6xl">
        <h1 className="text-2xl font-bold text-gradient-purple mb-1">Notifications & Messages</h1>
        <p className="text-muted-foreground text-sm mb-6">Stay connected with your travel updates and buddies</p>

        <div className="flex rounded-lg bg-secondary p-1 mb-6 max-w-sm">
          <button
            onClick={() => {
              setActiveTab("notifications");
              setShowChat(false);
            }}
            className={`flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 ${
              activeTab === "notifications" ? "bg-accent text-accent-foreground" : "text-muted-foreground"
            }`}
          >
            <Bell className="w-3.5 h-3.5" /> Notifications
            {getUnreadNotificationsCount() > 0 && (
              <span className="bg-destructive text-destructive-foreground text-[10px] px-1.5 py-0.5 rounded-full">
                {getUnreadNotificationsCount()}
              </span>
            )}
          </button>
          <button
            onClick={() => {
              setActiveTab("messages");
              setShowChat(false);
            }}
            className={`flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 ${
              activeTab === "messages" ? "bg-accent text-accent-foreground" : "text-muted-foreground"
            }`}
          >
            <MessageSquare className="w-3.5 h-3.5" /> Messages
            <span className="bg-primary text-primary-foreground text-[10px] px-1.5 py-0.5 rounded-full">{conversations.filter(c => c.unreadCount > 0).length}</span>
          </button>
        </div>

        {activeTab === "notifications" && !showChat && (
          <div className="card-glass p-5 max-w-3xl mx-auto">
            <div className="flex justify-between items-center mb-4">
              <h2 className="font-bold">Recent Notifications</h2>
              {getUnreadNotificationsCount() > 0 && (
                <button 
                  onClick={markAllAsRead}
                  className="text-xs text-accent hover:text-accent/80 transition-colors flex items-center gap-1"
                >
                  <CheckCheck className="w-3 h-3" />
                  Mark all as read
                </button>
              )}
            </div>
            <div className="space-y-2">
              {notificationsList.length === 0 ? (
                <div className="text-center py-8">
                  <Bell className="w-12 h-12 text-muted-foreground mx-auto mb-3 opacity-50" />
                  <p className="text-muted-foreground">No notifications yet</p>
                </div>
              ) : (
                notificationsList.map((n, i) => (
                  <div 
                    key={i} 
                    onClick={() => openNotification(n, i)}
                    className={`flex gap-3 p-4 rounded-lg transition-all cursor-pointer ${
                      n.unread 
                        ? "bg-secondary/30 hover:bg-secondary/50 border-l-4 border-accent" 
                        : "hover:bg-secondary/30 opacity-80"
                    }`}
                  >
                    <div className={`w-12 h-12 rounded-full ${n.color} flex items-center justify-center shrink-0`}>
                      <n.icon className="w-5 h-5" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 justify-between">
                        <div className="flex items-center gap-2">
                          <h3 className="text-sm font-semibold truncate">{n.title}</h3>
                          {n.unread && (
                            <span className="w-2 h-2 rounded-full bg-accent shrink-0 animate-pulse" />
                          )}
                        </div>
                        <span className="text-xs text-muted-foreground">{n.time}</span>
                      </div>
                      <p className="text-xs text-muted-foreground mt-1">{n.desc}</p>
                      <div className="flex items-center gap-1 mt-2">
                        <ExternalLink className="w-3 h-3 text-accent" />
                        <p className="text-xs text-accent">Tap to open</p>
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        )}

        {activeTab === "messages" && !showChat && (
          <div className="flex gap-4">
            <div className="w-full card-glass overflow-hidden flex flex-col">
              <div className="p-4 border-b border-border">
                <h2 className="font-bold">Conversations</h2>
                <p className="text-xs text-muted-foreground mt-1">Tap on any conversation to open chat</p>
              </div>
              <div className="flex-1 overflow-y-auto max-h-[calc(100vh-300px)]">
                {getConversationList().length === 0 ? (
                  <div className="p-8 text-center">
                    <MessageSquare className="w-10 h-10 text-muted-foreground mx-auto mb-3" />
                    <p className="text-muted-foreground text-sm">No messages yet. Connect with a local buddy to start chatting!</p>
                  </div>
                ) : (
                  getConversationList().map((conv) => (
                    <div
                      key={conv.buddyId}
                      onClick={() => openConversation(conv)}
                      className={`p-4 cursor-pointer transition-colors hover:bg-secondary/50 border-b border-border last:border-0 ${
                        conv.isFirstTime && conv.messages.length === 0 ? "bg-accent/5" : ""
                      }`}
                    >
                      <div className="flex items-center gap-3">
                        <img src={conv.buddyPhoto} alt={conv.buddyName} className="w-14 h-14 rounded-full object-cover" />
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-2">
                              <h3 className="font-semibold">{conv.buddyName}</h3>
                              {conv.isFirstTime && conv.messages.length === 0 && (
                                <span className="text-[10px] bg-accent/20 text-accent px-1.5 py-0.5 rounded-full">New</span>
                              )}
                            </div>
                            <span className="text-xs text-muted-foreground">{formatTime(conv.lastMessageTime)}</span>
                          </div>
                          <p className="text-sm text-muted-foreground truncate mt-1">
                            {conv.messages.length === 0 ? "✨ New conversation - Say hi!" : conv.lastMessage}
                          </p>
                        </div>
                        {conv.unreadCount > 0 && (
                          <div className="w-6 h-6 rounded-full bg-accent flex items-center justify-center animate-pulse">
                            <span className="text-[10px] font-bold text-accent-foreground">{conv.unreadCount}</span>
                          </div>
                        )}
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>
        )}

        {/* Notification Detail Modal - Removed View Details button */}
        {showNotificationModal && selectedNotification && (
          <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4">
            <div className="w-full max-w-md bg-card rounded-xl shadow-xl overflow-hidden">
              <div className={`p-4 ${selectedNotification.color} flex items-center justify-between`}>
                <div className="flex items-center gap-3">
                  <selectedNotification.icon className="w-6 h-6" />
                  <h3 className="font-bold">Notification</h3>
                </div>
                <button 
                  onClick={() => setShowNotificationModal(false)}
                  className="p-1 hover:bg-black/10 rounded transition-colors"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>
              
              <div className="p-6">
                <h2 className="text-xl font-bold mb-2">{selectedNotification.title}</h2>
                <p className="text-muted-foreground text-sm mb-4">{selectedNotification.desc}</p>
                <p className="text-xs text-muted-foreground mb-6">{selectedNotification.time}</p>
                
                <button
                  onClick={handleNotificationAction}
                  className="w-full btn-accent py-2 rounded-lg"
                >
                  Close
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Chat Modal */}
        {showChat && selectedBuddy && (
          <div className="fixed inset-0 bg-background/95 backdrop-blur-sm z-50 flex items-center justify-center p-4">
            <div className="w-full max-w-2xl h-[80vh] card-glass overflow-hidden flex flex-col">
              <div className="p-4 border-b border-border flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <button 
                    onClick={closeChat}
                    className="p-1 hover:bg-secondary rounded transition-colors"
                  >
                    <ArrowLeft className="w-5 h-5" />
                  </button>
                  <img src={selectedBuddy.buddyPhoto} alt={selectedBuddy.buddyName} className="w-10 h-10 rounded-full object-cover" />
                  <div>
                    <h3 className="font-semibold">{selectedBuddy.buddyName}</h3>
                    <p className="text-xs text-green-500">● Online</p>
                  </div>
                </div>
                <button 
                  onClick={closeChat}
                  className="p-1 hover:bg-secondary rounded transition-colors"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>

              {showWelcome && selectedBuddy.isFirstTime && selectedBuddy.messages.length === 0 && (
                <div className="p-6 bg-gradient-to-r from-accent/10 to-primary/10 border-b border-border">
                  <div className="flex items-center gap-3 mb-3">
                    <Sparkles className="w-8 h-8 text-accent" />
                    <div>
                      <h3 className="font-bold text-lg">Start your conversation! 🎉</h3>
                      <p className="text-sm text-muted-foreground">
                        This is your first time chatting with {selectedBuddy.buddyName}
                      </p>
                    </div>
                  </div>
                  <p className="text-sm mb-4">
                    Say hello and start planning your Egyptian adventure together!
                  </p>
                  
                  <div className="mt-4">
                    <p className="text-xs text-muted-foreground mb-2">Suggested messages:</p>
                    <div className="flex flex-wrap gap-2">
                      {suggestedMessages.map((msg, idx) => (
                        <button
                          key={idx}
                          onClick={() => handleSuggestedMessage(msg)}
                          className="text-xs bg-secondary hover:bg-accent/20 px-3 py-1.5 rounded-full transition-colors"
                        >
                          {msg}
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              )}

              <div className="flex-1 overflow-y-auto p-4 space-y-3">
                {selectedBuddy.messages.length === 0 && !showWelcome ? (
                  <div className="flex flex-col items-center justify-center h-full text-center">
                    <MessageSquare className="w-12 h-12 text-muted-foreground mb-3 opacity-50" />
                    <p className="text-muted-foreground">No messages yet</p>
                    <p className="text-xs text-muted-foreground mt-1">Send a message to start chatting!</p>
                  </div>
                ) : (
                  selectedBuddy.messages.map((msg: Message) => (
                    <div key={msg.id} className={`flex ${msg.sender === "user" ? "justify-end" : "justify-start"}`}>
                      <div className={`max-w-[70%] p-3 rounded-lg ${
                        msg.sender === "user" 
                          ? "bg-accent text-accent-foreground" 
                          : "bg-secondary"
                      }`}>
                        <p className="text-sm">{msg.text}</p>
                        <p className="text-[10px] opacity-70 mt-1">
                          {msg.timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </p>
                      </div>
                    </div>
                  ))
                )}
              </div>

              <div className="p-4 border-t border-border">
                <div className="flex gap-2">
                  <input
                    type="text"
                    value={messageInput}
                    onChange={(e) => setMessageInput(e.target.value)}
                    onKeyPress={(e) => e.key === "Enter" && handleSendMessage()}
                    placeholder={showWelcome ? "Type your first message..." : "Type a message..."}
                    className="flex-1 px-4 py-2 bg-secondary rounded-lg focus:outline-none focus:ring-1 focus:ring-accent text-sm"
                    autoFocus
                  />
                  <button
                    onClick={handleSendMessage}
                    className="p-2 bg-accent rounded-lg hover:bg-accent/80 transition-colors"
                  >
                    <Send className="w-5 h-5 text-accent-foreground" />
                  </button>
                </div>
                {showWelcome && (
                  <p className="text-xs text-muted-foreground mt-2 text-center">
                    ✨ First message? Make it special!
                  </p>
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default Messages;