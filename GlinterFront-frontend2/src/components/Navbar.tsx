import { useState, useEffect } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { Compass, Users, MessageSquare, Info, Settings, User, MapPin, Map, LogIn, Menu, X } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import { authStorage } from "@/shared/lib/auth";
import { authApi } from "@/shared/services/api-auth";
import { getRoleHome } from "@/shared/lib/auth-routing";
import { chatApi, notificationsApi } from "@/shared/services/api-communication";

const navItems = [
  { path: "/explore", label: "Explore", icon: Compass },
  { path: "/where-to-stay", label: "Where to Stay", icon: MapPin },
  { path: "/where-to-go", label: "Where to Go", icon: Map },
  { path: "/local-buddies", label: "Local Buddies", icon: Users },
  { path: "/messages", label: "Messages", icon: MessageSquare },
  { path: "/about", label: "About", icon: Info },
];

const Navbar = ({ solid }: { solid?: boolean }) => {
  const location = useLocation();
  const navigate = useNavigate();
  const [scrolled, setScrolled] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [isLoggedIn, setIsLoggedIn] = useState(() => authStorage.isAuthenticated());
  const [isAdmin, setIsAdmin] = useState(() => authStorage.isAdmin());
  const [communicationUnread, setCommunicationUnread] = useState(0);
  const accountHome = getRoleHome(authStorage.getUser()?.roles ?? []);

  useEffect(() => {
    const check = () => {
      setIsLoggedIn(authStorage.isAuthenticated());
      setIsAdmin(authStorage.isAdmin());
    };
    window.addEventListener("storage", check);
    window.addEventListener("auth-change", check);
    return () => {
      window.removeEventListener("storage", check);
      window.removeEventListener("auth-change", check);
    };
  }, []);

  useEffect(() => {
    if (!isLoggedIn) {
      setCommunicationUnread(0);
      return;
    }
    Promise.all([notificationsApi.getNotifications(1, 1), chatApi.getThreads()])
      .then(([notificationPage, threads]) => {
        setCommunicationUnread(
          notificationPage.unreadCount +
          threads.reduce((total, thread) => total + thread.unreadCount, 0),
        );
      })
      .catch((error: unknown) => {
        console.error("Could not load communication unread count.", error);
      });

    const updateUnread = (event: Event) => {
      setCommunicationUnread((event as CustomEvent<number>).detail);
    };
    window.addEventListener("communication-unread-change", updateUnread);
    return () => window.removeEventListener("communication-unread-change", updateUnread);
  }, [isLoggedIn]);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 0);
    window.addEventListener("scroll", onScroll);
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  const handleLogout = async () => {
    try {
      await authApi.logout();
    } catch (error) {
      console.error("Backend logout failed; clearing the local session.", error);
    }
    authStorage.clearAll();
    setIsLoggedIn(false);
    window.dispatchEvent(new Event("auth-change"));
    navigate("/");
  };

  return (
    <nav className="sticky top-0 z-50 p-4">
      <div className="container mx-auto max-w-7xl">
        <div className={`rounded-full px-6 py-3 flex items-center justify-between transition-all duration-300 ${scrolled && !solid ? 'liquid-glass' : ''}`}>
          <Link to="/" className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-full bg-brand-gold flex items-center justify-center text-brand-dark font-bold text-sm">
              G
            </div>
            <span className="text-xl font-bold tracking-wide">Glinter</span>
          </Link>

          <div className="hidden md:flex items-center gap-6 text-sm font-medium">
            {navItems.map((item) => {
              const isActive = location.pathname === item.path;
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  className={`nav-link flex items-center gap-2 ${isActive ? "active" : "text-gray-300"}`}
                >
                  <item.icon className="w-4 h-4" />
                  {item.label}
                  {item.path === "/messages" && communicationUnread > 0 && (
                    <span className="rounded-full bg-accent px-1.5 py-0.5 text-[10px] text-accent-foreground">
                      {communicationUnread}
                    </span>
                  )}
                </Link>
              );
            })}
          </div>

          <div className="flex items-center gap-4">
            {isAdmin && (
              <Link
                to="/admin"
                className="hidden lg:flex items-center gap-2 px-3 py-2 text-sm text-gray-400 hover:text-white transition-colors"
              >
                <Settings className="w-4 h-4" />
                Admin
              </Link>
            )}
            {isLoggedIn ? (
              <>
                <Link to={accountHome} className="w-9 h-9 rounded-full bg-brand-purple flex items-center justify-center border-2 border-brand-glassBorder hover:opacity-80 transition-opacity">
                  <User className="w-4 h-4 text-white" />
                </Link>
                <button
                  onClick={() => { handleLogout(); }}
                  className="hidden md:flex items-center gap-2 px-3 py-2 text-sm font-medium text-brand-gold hover:opacity-80 transition-opacity"
                >
                  <LogIn className="w-4 h-4" />
                  Logout
                </button>
              </>
            ) : (
              <Link
                to="/auth"
                className="hidden md:flex items-center gap-2 px-3 py-2 text-sm font-medium text-brand-gold hover:opacity-80 transition-opacity"
              >
                <LogIn className="w-4 h-4" />
                Login
              </Link>
            )}
            <button
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              className="md:hidden p-2 rounded-lg hover:bg-white/5 transition-colors"
            >
              {mobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
            </button>
          </div>
        </div>
      </div>

      <AnimatePresence>
        {mobileMenuOpen && (
          <motion.div
            initial={{ opacity: 0, y: -10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            className="md:hidden container mx-auto max-w-7xl px-4 mt-2"
          >
            <div className="liquid-glass rounded-2xl p-4 space-y-2">
              {navItems.map((item) => {
                const isActive = location.pathname === item.path;
                return (
                  <Link
                    key={item.path}
                    to={item.path}
                    onClick={() => setMobileMenuOpen(false)}
                    className={`flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-all ${
                      isActive ? "bg-brand-purple/20 text-brand-gold" : "text-gray-300 hover:bg-white/5"
                    }`}
                  >
                    <item.icon className="w-5 h-5" />
                    {item.label}
                    {item.path === "/messages" && communicationUnread > 0 && (
                      <span className="ml-auto rounded-full bg-accent px-2 py-0.5 text-[10px] text-accent-foreground">
                        {communicationUnread}
                      </span>
                    )}
                  </Link>
                );
              })}
              <div className="pt-2 border-t border-white/10 space-y-2">
                {isLoggedIn && (
                  <>
                    {isAdmin && (
                      <Link to="/admin" onClick={() => setMobileMenuOpen(false)} className="flex items-center gap-3 px-4 py-3 rounded-lg text-sm text-gray-300 hover:bg-white/5 transition-all">
                        <Settings className="w-5 h-5" />
                        Admin
                      </Link>
                    )}
                    <Link to={accountHome} onClick={() => setMobileMenuOpen(false)} className="flex items-center gap-3 px-4 py-3 rounded-lg text-sm text-gray-300 hover:bg-white/5 transition-all">
                      <User className="w-5 h-5" />
                      Account
                    </Link>
                  </>
                )}
                <Link
                  to="/auth"
                  onClick={(e) => {
                    if (isLoggedIn) { e.preventDefault(); setMobileMenuOpen(false); handleLogout(); }
                  }}
                  className="flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium text-brand-gold hover:opacity-80 transition-all"
                >
                  <LogIn className="w-5 h-5" />
                  {isLoggedIn ? "Logout" : "Login"}
                </Link>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </nav>
  );
};

export default Navbar;
