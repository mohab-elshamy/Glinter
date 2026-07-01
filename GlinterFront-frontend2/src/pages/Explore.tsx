import { useState, useRef, useEffect } from "react";
import {
  Search, Sparkles, ArrowRight, Map, Compass, Sun, Waves,
  Mountain, Landmark, Camera, TrendingUp, Star, Users,
  Clock, ChevronRight, BookOpen, Globe, Heart, Navigation
} from "lucide-react";
import { motion, useScroll, useTransform, useInView, AnimatePresence } from "framer-motion";
import { useNavigate } from "react-router-dom";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import { toast } from "sonner";
import heroImg from "@/assets/hero-egypt.jpg";
import luxorImg from "@/assets/luxor.jpg";
import cairoImg from "@/assets/cairo.jpg";
import aswanImg from "@/assets/aswan.jpg";
import alexandriaImg from "@/assets/alexandria.jpg";

const experiences = [
  {
    title: "Ancient Wonders",
    desc: "Stand before the last surviving Wonder of the World",
    gradient: "from-amber-600/40 to-orange-900/60",
    icon: Landmark,
    color: "text-amber-400",
    bgGlow: "bg-amber-500/10",
    image: cairoImg,
    link: "/where-to-go?category=history",
  },
  {
    title: "Nile Cruises",
    desc: "Sail through 5,000 years of history on the world's longest river",
    gradient: "from-emerald-600/40 to-teal-900/60",
    icon: Waves,
    color: "text-emerald-400",
    bgGlow: "bg-emerald-500/10",
    image: luxorImg,
    link: "/where-to-go?category=cruise",
  },
  {
    title: "Desert Adventures",
    desc: "Camp under the stars in the Sahara's golden dunes",
    gradient: "from-yellow-600/40 to-red-900/60",
    icon: Sun,
    color: "text-yellow-400",
    bgGlow: "bg-yellow-500/10",
    image: aswanImg,
    link: "/where-to-go?category=adventure",
  },
  {
    title: "Red Sea Escapes",
    desc: "Dive into crystal waters among vibrant coral gardens",
    gradient: "from-blue-600/40 to-cyan-900/60",
    icon: Mountain,
    color: "text-blue-400",
    bgGlow: "bg-blue-500/10",
    image: alexandriaImg,
    link: "/where-to-go?category=beach",
  },
];

const spotlightDestinations = [
  { name: "Valley of the Kings", location: "Luxor", desc: "Unearth the tombs of pharaohs in the ancient capital of Thebes", rating: 4.9, image: luxorImg, tag: "Must Visit" },
  { name: "Pyramids of Giza", location: "Cairo", desc: "The last surviving Wonder of the Ancient World", rating: 4.9, image: cairoImg, tag: "Iconic" },
  { name: "Abu Simbel", location: "Aswan", desc: "Ramesses II's magnificent temple relocated piece by piece", rating: 4.8, image: aswanImg, tag: "Hidden Gem" },
];

const quickLinks = [
  { label: "Where to Stay", icon: Map, color: "text-purple-400", bg: "bg-purple-500/10", link: "/where-to-stay" },
  { label: "Local Buddies", icon: Users, color: "text-blue-400", bg: "bg-blue-500/10", link: "/local-buddies" },
  { label: "Trip Planner", icon: Sparkles, color: "text-yellow-400", bg: "bg-yellow-500/10", link: "/where-to-go" },
  { label: "Travel Guide", icon: BookOpen, color: "text-emerald-400", bg: "bg-emerald-500/10", link: "/where-to-go?guide=true" },
];

const floatingGlyphs = [
  { glyph: "𓋹", size: 28, x: 12, y: 18, delay: 0 },
  { glyph: "𓆣", size: 22, x: 78, y: 12, delay: 0.6 },
  { glyph: "𓃠", size: 18, x: 85, y: 72, delay: 1.2 },
  { glyph: "𓋴", size: 24, x: 20, y: 65, delay: 0.4 },
  { glyph: "𓆑", size: 16, x: 55, y: 22, delay: 1.8 },
  { glyph: "𓂀", size: 20, x: 68, y: 55, delay: 0.9 },
  { glyph: "𓊹", size: 26, x: 32, y: 78, delay: 1.5 },
  { glyph: "𓎟", size: 15, x: 90, y: 40, delay: 0.2 },
];

const GlowOrb = ({ color, size, pos, delay }: { color: string; size: number; pos: [string, string]; delay: number }) => (
  <motion.div
    className="absolute rounded-full pointer-events-none"
    style={{ width: size, height: size, left: pos[0], top: pos[1], background: `radial-gradient(circle, ${color} 0%, transparent 70%)` }}
    animate={{ x: [0, 40, -25, 15, 0], y: [0, -35, 20, 30, 0], scale: [1, 1.12, 0.92, 1.06, 1] }}
    transition={{ duration: 10 + delay, repeat: Infinity, ease: "easeInOut", delay }}
  />
);

const Counter = ({ value, label, suffix = "" }: { value: number; label: string; suffix?: string }) => {
  const [count, setCount] = useState(0);
  const ref = useRef(null);
  const inView = useInView(ref, { once: true });
  useEffect(() => {
    if (!inView) return;
    let start = 0; const end = value; const dur = 2000;
    const step = Math.ceil(end / (dur / 16));
    const t = setInterval(() => {
      start += step; if (start >= end) { setCount(end); clearInterval(t); } else setCount(start);
    }, 16);
    return () => clearInterval(t);
  }, [inView, value]);
  return (
    <div ref={ref} className="text-center">
      <div className="text-3xl md:text-4xl font-bold text-white">{count}{suffix}</div>
      <div className="text-xs text-gray-400 mt-1 uppercase tracking-wider">{label}</div>
    </div>
  );
};

const Explore = () => {
  const [query, setQuery] = useState("");
  const [hoveredExp, setHoveredExp] = useState<number | null>(null);
  const navigate = useNavigate();
  const heroRef = useRef(null);
  const expRef = useRef(null);
  const statsRef = useRef(null);

  const { scrollYProgress } = useScroll({ target: heroRef, offset: ["start start", "end start"] });
  const heroY = useTransform(scrollYProgress, [0, 1], ["0%", "25%"]);
  const heroO = useTransform(scrollYProgress, [0, 0.7], [1, 0]);

  const expInView = useInView(expRef, { once: true, margin: "-80px" });
  const statsInView = useInView(statsRef, { once: true, margin: "-80px" });

  const handleSearch = () => {
    if (query.trim()) { toast.success(`Discovering "${query.trim()}"...`); navigate("/where-to-go"); }
    else toast.info("Where would you like to go?");
  };

  return (
    <div className="min-h-screen bg-background overflow-x-hidden">
      <Navbar />

      {/* Hero */}
      <section ref={heroRef} className="relative h-screen flex items-center justify-center overflow-hidden">
        <motion.div className="absolute inset-0" style={{ y: heroY }}>
          <img src={heroImg} alt="Egypt" className="w-full h-full object-cover opacity-45" />
          <div className="absolute inset-0 bg-gradient-to-b from-background/30 via-background/50 to-background" />
        </motion.div>

        <GlowOrb color="hsl(270 80% 60% / 0.12)" size={550} pos={["15%", "15%"]} delay={0} />
        <GlowOrb color="hsl(45 95% 55% / 0.08)" size={450} pos={["65%", "25%"]} delay={2} />
        <GlowOrb color="hsl(190 80% 60% / 0.06)" size={400} pos={["40%", "65%"]} delay={4} />
        <div className="noise-overlay" />

        {floatingGlyphs.map((g, i) => (
          <motion.div
            key={i}
            className="absolute text-white/[0.04] select-none"
            style={{ left: `${g.x}%`, top: `${g.y}%`, fontSize: g.size }}
            animate={{ y: [0, -25, 8, -18, 0], x: [0, 15, -12, 8, 0], rotate: [0, 12, -6, 10, 0], opacity: [0.04, 0.1, 0.04] }}
            transition={{ duration: 7 + g.delay, repeat: Infinity, ease: "easeInOut", delay: g.delay }}
          >{g.glyph}</motion.div>
        ))}

        <motion.div className="relative z-10 container mx-auto px-4" style={{ opacity: heroO }}>
          <div className="max-w-4xl mx-auto text-center">
            <motion.div
              initial={{ opacity: 0, y: 30 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.8, ease: [0.23, 1, 0.32, 1] }}
            >
              <motion.div
                initial={{ scale: 0.8, opacity: 0 }}
                animate={{ scale: 1, opacity: 1 }}
                transition={{ delay: 0.2, duration: 0.5 }}
                className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-white/5 border border-white/10 text-xs text-gray-300 mb-6"
              >
                <Globe className="w-3.5 h-3.5 text-gold" />
                Discover the Land of the Pharaohs
              </motion.div>

              <h1 className="text-5xl md:text-7xl lg:text-8xl font-bold leading-[1.1] mb-4">
                <motion.span
                  className="block text-white font-light"
                  initial={{ opacity: 0, x: -40 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ duration: 0.6, delay: 0.3 }}
                >
                  Your Journey to
                </motion.span>
                <motion.span
                  className="block text-shimmer mt-2"
                  initial={{ opacity: 0, x: 40 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ duration: 0.6, delay: 0.5 }}
                >
                  Ancient Egypt
                </motion.span>
              </h1>

              <motion.p
                className="text-base md:text-lg text-gray-400 max-w-xl mx-auto mb-10 h-7"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ duration: 0.5, delay: 0.8 }}
              >
                <span className="typewriter">Beyond the guidebook — travel like a local</span>
              </motion.p>

              {/* Search */}
              <motion.div
                className="max-w-xl mx-auto mb-10"
                initial={{ opacity: 0, y: 20, scale: 0.95 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                transition={{ duration: 0.5, delay: 1.1 }}
              >
                <div className="relative group">
                  <div className="absolute -inset-0.5 bg-gradient-to-r from-purple-600 via-gold to-purple-600 rounded-full opacity-0 group-hover:opacity-75 blur transition-all duration-500 bg-[length:200%_100%] animate-shimmer" />
                  <div className="relative bg-white/10 backdrop-blur-xl border border-white/10 rounded-full p-1.5 flex items-center">
                    <Compass className="w-5 h-5 text-purple-400 ml-4 shrink-0" />
                    <input
                      value={query}
                      onChange={(e) => setQuery(e.target.value)}
                      onKeyDown={(e) => e.key === "Enter" && handleSearch()}
                      placeholder="Search destinations, experiences..."
                      className="flex-1 bg-transparent text-sm text-white placeholder:text-gray-500 focus:outline-none px-3"
                    />
                    <button
                      onClick={handleSearch}
                      className="bg-purple-600 hover:bg-purple-500 text-white rounded-full py-2.5 px-6 flex items-center gap-2 transition-all shrink-0 group/search"
                    >
                      <span>Explore</span>
                      <ArrowRight className="w-4 h-4 group-hover/search:translate-x-0.5 transition-transform" />
                    </button>
                  </div>
                </div>
              </motion.div>

              {/* Quick Links */}
              <motion.div
                className="flex flex-wrap justify-center gap-3"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.5, delay: 1.4 }}
              >
                {quickLinks.map((link, i) => (
                  <motion.button
                    key={link.label}
                    initial={{ opacity: 0, scale: 0.8 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ delay: 1.5 + i * 0.08, duration: 0.3 }}
                    whileHover={{ scale: 1.05, y: -2 }}
                    whileTap={{ scale: 0.95 }}
                    onClick={() => navigate(link.link)}
                    className="flex items-center gap-2 px-4 py-2 rounded-full bg-white/5 border border-white/10 text-sm text-gray-300 hover:bg-white/10 hover:text-white hover:border-white/20 transition-all"
                  >
                    <link.icon className={`w-3.5 h-3.5 ${link.color}`} />
                    {link.label}
                  </motion.button>
                ))}
              </motion.div>
            </motion.div>
          </div>
        </motion.div>

        {/* Scroll indicator */}
        <motion.div
          className="absolute bottom-10 left-1/2 -translate-x-1/2 flex flex-col items-center gap-2 text-white/25"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 2.5, duration: 0.5 }}
        >
          <span className="text-[10px] tracking-[0.3em] uppercase">Discover</span>
          <ChevronRight className="w-4 h-4 -rotate-90 scroll-indicator" />
        </motion.div>
      </section>

      {/* Stats Bar */}
      <section ref={statsRef} className="py-16 relative">
        <div className="absolute inset-0 bg-gradient-to-r from-purple-900/10 via-transparent to-gold/10" />
        <div className="container mx-auto px-4 relative z-10">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8 max-w-3xl mx-auto">
            {[
              { value: 50, label: "Destinations", suffix: "+" },
              { value: 200, label: "Experiences", suffix: "+" },
              { value: 500, label: "Local Guides", suffix: "+" },
              { value: 15, label: "Years of History", suffix: "k+" },
            ].map((s, i) => (
              <motion.div
                key={s.label}
                initial={{ opacity: 0, y: 30 }}
                animate={statsInView ? { opacity: 1, y: 0 } : {}}
                transition={{ delay: i * 0.12, duration: 0.5 }}
                className="relative"
              >
                <Counter value={s.value} label={s.label} suffix={s.suffix} />
              </motion.div>
            ))}
          </div>
        </div>
      </section>

      {/* Experiences Grid */}
      <section ref={expRef} className="container mx-auto px-4 py-16">
        <motion.div
          className="flex items-center justify-between mb-10"
          initial={{ opacity: 0, y: 20 }}
          animate={expInView ? { opacity: 1, y: 0 } : {}}
          transition={{ duration: 0.5 }}
        >
          <div>
            <div className="section-badge mb-3 inline-flex items-center gap-2">
              <Compass className="w-3.5 h-3.5" />
              Choose Your Path
            </div>
            <h2 className="text-3xl md:text-4xl font-bold">
              Explore by <span className="text-gradient-orange">Experience</span>
            </h2>
            <p className="text-sm text-muted-foreground mt-2">Egypt has many faces — which one calls to you?</p>
          </div>
          <motion.button
            whileHover={{ x: 3 }}
            onClick={() => navigate("/where-to-go")}
            className="hidden md:flex items-center gap-1 text-sm text-gold hover:text-gold/80 transition-colors"
          >
            View All <ArrowRight className="w-3.5 h-3.5" />
          </motion.button>
        </motion.div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          {experiences.map((exp, i) => (
            <motion.div
              key={exp.title}
              initial={{ opacity: 0, y: 40 }}
              animate={expInView ? { opacity: 1, y: 0 } : {}}
              transition={{ duration: 0.5, delay: i * 0.12, ease: [0.23, 1, 0.32, 1] }}
              onMouseEnter={() => setHoveredExp(i)}
              onMouseLeave={() => setHoveredExp(null)}
              onClick={() => navigate(exp.link)}
              className="relative h-[320px] md:h-[380px] rounded-3xl overflow-hidden group cursor-pointer"
            >
              <motion.img
                src={exp.image}
                alt={exp.title}
                className="absolute inset-0 w-full h-full object-cover"
                animate={{ scale: hoveredExp === i ? 1.08 : 1 }}
                transition={{ duration: 0.7 }}
              />
              <div className={`absolute inset-0 bg-gradient-to-t ${exp.gradient} transition-opacity duration-500`} />
              <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent" />

              {/* Number */}
              <div className="absolute top-5 left-5 text-6xl font-bold text-white/5 select-none">
                0{i + 1}
              </div>

              {/* Icon */}
              <motion.div
                className={`absolute top-5 right-5 w-12 h-12 rounded-2xl ${exp.bgGlow} backdrop-blur-md flex items-center justify-center border border-white/10`}
                animate={{ rotate: hoveredExp === i ? [0, -8, 8, 0] : 0 }}
                transition={{ duration: 0.4 }}
              >
                <exp.icon className={`w-5 h-5 ${exp.color}`} />
              </motion.div>

              <div className="absolute bottom-0 left-0 right-0 p-6 md:p-8">
                <motion.div
                  animate={{ y: hoveredExp === i ? -4 : 0 }}
                  transition={{ duration: 0.3 }}
                >
                  <h3 className="text-2xl md:text-3xl font-bold text-white mb-2">{exp.title}</h3>
                  <p className="text-sm text-gray-300 max-w-md">{exp.desc}</p>
                </motion.div>

                <motion.div
                  className="mt-4 flex items-center gap-1.5 text-xs text-white/60"
                  animate={{ opacity: hoveredExp === i ? 1 : 0.6 }}
                >
                  <span>Explore Now</span>
                  <ArrowRight className="w-3 h-3" />
                </motion.div>
              </div>

              {/* Hover border glow */}
              <div className="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-500 pointer-events-none rounded-3xl ring-1 ring-white/10" />
            </motion.div>
          ))}
        </div>
      </section>

      {/* Spotlight Destinations */}
      <section className="container mx-auto px-4 py-20">
        <motion.div
          className="flex items-center justify-between mb-10"
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
        >
          <div>
            <div className="section-badge mb-3 inline-flex items-center gap-2">
              <Star className="w-3.5 h-3.5" />
              Editor's Pick
            </div>
            <h2 className="text-3xl md:text-4xl font-bold">
              Spotlight <span className="text-gradient-purple">Destinations</span>
            </h2>
          </div>
        </motion.div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {spotlightDestinations.map((d, i) => (
            <motion.div
              key={d.name}
              initial={{ opacity: 0, y: 30 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ delay: i * 0.15, duration: 0.5 }}
              whileHover={{ y: -6 }}
              onClick={() => navigate(`/where-to-stay?city=${d.location}`)}
              className="group cursor-pointer"
            >
              <div className="relative h-[280px] rounded-2xl overflow-hidden mb-4">
                <img
                  src={d.image}
                  alt={d.name}
                  className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700"
                />
                <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent" />

                {/* Tag */}
                <div className="absolute top-4 left-4 bg-black/50 backdrop-blur-md rounded-full px-3 py-1">
                  <span className="text-[10px] uppercase tracking-wider text-gold font-medium">{d.tag}</span>
                </div>

                {/* Rating */}
                <div className="absolute top-4 right-4 bg-black/50 backdrop-blur-md rounded-full px-3 py-1 flex items-center gap-1">
                  <Star className="w-3 h-3 fill-yellow-400 text-yellow-400" />
                  <span className="text-xs font-bold text-white">{d.rating}</span>
                </div>

                <div className="absolute bottom-0 left-0 right-0 p-5">
                  <p className="text-[10px] uppercase tracking-[0.15em] text-gray-400 mb-1">{d.location}, Egypt</p>
                  <h3 className="text-xl font-bold text-white">{d.name}</h3>
                </div>
              </div>
              <p className="text-sm text-muted-foreground leading-relaxed">{d.desc}</p>
            </motion.div>
          ))}
        </div>
      </section>

      {/* CTA */}
      <section className="container mx-auto px-4 py-20">
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          whileInView={{ opacity: 1, scale: 1 }}
          viewport={{ once: true }}
          transition={{ duration: 0.6 }}
          className="relative rounded-3xl overflow-hidden"
        >
          <div className="absolute inset-0 bg-gradient-to-br from-purple-900/40 via-background to-gold/20" />
          <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top_right,hsl(270,80%,60%,0.1),transparent_60%)]" />
          <div className="relative px-8 py-16 md:py-20 text-center">
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ delay: 0.2 }}
            >
              <div className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-white/5 border border-white/10 text-xs text-gray-300 mb-5">
                <Navigation className="w-3.5 h-3.5 text-gold" />
                Ready for Adventure?
              </div>
              <h2 className="text-3xl md:text-5xl font-bold text-white mb-4">
                Your Egyptian Journey <br />
                <span className="text-shimmer">Starts Here</span>
              </h2>
              <p className="text-gray-400 max-w-md mx-auto mb-8">
                Build a flexible itinerary from real experiences based on your interests, budget, and travel style.
              </p>
              <motion.button
                whileHover={{ scale: 1.03 }}
                whileTap={{ scale: 0.97 }}
                onClick={() => navigate("/where-to-go")}
                className="inline-flex items-center gap-2 bg-purple-600 hover:bg-purple-500 text-white rounded-full px-8 py-3.5 font-medium transition-all shadow-lg shadow-purple-600/25"
              >
                <Sparkles className="w-4 h-4" />
                Plan My Trip
                <ArrowRight className="w-4 h-4" />
              </motion.button>
            </motion.div>
          </div>
        </motion.div>
      </section>

      <Footer />
    </div>
  );
};

export default Explore;
