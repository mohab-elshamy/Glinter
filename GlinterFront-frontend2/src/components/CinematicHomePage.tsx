import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Lenis, useLenis } from "lenis/react";
import { motion } from "framer-motion";
import {
  Sparkles,
  ChevronDown,
  Search,
  Leaf,
  Landmark,
  Mountain,
  Users,
  Diamond,
  ChevronRight,
  Star,
  ArrowRight,
  Compass,
  Map,
  Eye,
  BookOpen,
  Award,
  CalendarDays,
} from "lucide-react";
import Navbar from "./Navbar";

/* ============================================================
   ASSET IMPORTS
   ============================================================ */

// Card Images (A-F) — keep as-is, served from /public
const imgCard1 = "/chatgpt-assets-A_enhanced_x4.png";
const imgCard2 = "/chatgpt-assets-B_enhanced_x4.png";
const imgCard3 = "/chatgpt-assets-C_enhanced_x4.png";
const imgCard4 = "/chatgpt-assets-D_enhanced_x4.png";
const imgCard5 = "/chatgpt-assets-E_enhanced_x4.png";
const imgCard6 = "/chatgpt-assets-F_enhanced_x4.png";

// Section Images
const img3DPortal = "/chatgpt-assets-1_enhanced_x4.png"; // Section 2: Pyramids + purple ring + boat
const bgStats = "/chatgpt-assets-2_enhanced_x4.png"; // Stats background: night pyramids + network
const bgConstellations = "/chatgpt-assets-3_enhanced_x4.png"; // Features bg: constellation lines
const imgCTAPortal = "/chatgpt-assets-4_enhanced_x4.png"; // CTA: man in portal panorama

import luxorImg from "@/assets/luxor.jpg";
import cairoImg from "@/assets/cairo.jpg";
import aswanImg from "@/assets/aswan.jpg";
import alexandriaImg from "@/assets/alexandria.jpg";

const FRAMES_COUNT = 483;

function pad(n: number, len = 3) {
  return String(n).padStart(len, "0");
}

function framesSrc(index: number) {
  return `/framesGX/First-video-main_frame_${pad(index)}.jpg`;
}

/* ============================================================
   FOOTER COMPONENT (inline, self-contained)
   ============================================================ */
function Footer() {
  const navigate = useNavigate();

  return (
    <footer className="w-full border-t border-white/5 bg-[#0B0B13]/80 backdrop-blur-sm py-5 px-6">
      <div className="max-w-[1200px] mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
        {/* Logo */}
        <div className="flex items-center gap-2">
          <div className="w-6 h-6 rounded-md bg-[#DDA73B] flex items-center justify-center">
            <span className="text-[#0B0B13] font-black text-[10px]">G</span>
          </div>
          <span className="text-white font-bold text-sm tracking-tight">Glinter</span>
        </div>

        {/* Links */}
        <div className="flex items-center gap-6">
          {["About", "Explore", "Hotels", "Guides"].map((link) => (
            <button
              key={link}
              onClick={() => navigate(`/${link.toLowerCase()}`)}
              className="text-[11px] text-gray-400 hover:text-white transition-colors"
            >
              {link}
            </button>
          ))}
        </div>

        {/* Copyright */}
        <p className="text-[10px] text-gray-500">
          &copy; 2024 Glinter AI-powered travel for Egypt.
        </p>
      </div>
    </footer>
  );
}

const journeyFeatures = [
  { title: "AI-Powered Itineraries", desc: "Personalized travel plans crafted by AI that learns your preferences, budget, and travel style.", img: "/chatgpt-assets-A_enhanced_x4.png", color: "#9333EA" },
  { title: "Interactive Map Experience", desc: "Navigate Egypt with heatmaps showing safety, comfort, and popularity for every neighborhood.", img: "/chatgpt-assets-B_enhanced_x4.png", color: "#FACC15" },
  { title: "Local Guide Connections", desc: "Connect with verified local guides who know the hidden gems and authentic experiences.", img: "/chatgpt-assets-C_enhanced_x4.png", color: "#06B6D4" },
  { title: "Curated Destinations", desc: "Hand-picked destinations from the Pyramids to Siwa Oasis, each with insider knowledge.", img: "/chatgpt-assets-D_enhanced_x4.png", color: "#FACC15" },
  { title: "Immersive Previews", desc: "360-degree previews and cinematic flythroughs before you even book your trip.", img: "/chatgpt-assets-E_enhanced_x4.png", color: "#EF4444" },
  { title: "Smart Trip Planning", desc: "Real-time weather, crowd levels, and best-time-to-visit intelligence built right in.", img: "/chatgpt-assets-F_enhanced_x4.png", color: "#F97316" },
];

function TimelineProgressLine() {
  const gradient = 'linear-gradient(to bottom, #9333EA 0%, #9333EA 12%, #FACC15 22%, #FACC15 28%, #06B6D4 38%, #06B6D4 46%, #FACC15 54%, #FACC15 62%, #EF4444 72%, #EF4444 78%, #F97316 88%, #F97316 100%)';

  return (
    <motion.div
      initial={{ opacity: 0 }}
      whileInView={{ opacity: 1 }}
      viewport={{ once: false, margin: "-100px" }}
      transition={{ duration: 0.6 }}
      className="absolute top-0 left-1/2 -translate-x-1/2 w-[5px] h-full z-0"
      style={{
        background: gradient,
        WebkitMaskImage: 'repeating-linear-gradient(to bottom, black 0px, black 20px, transparent 20px, transparent 40px)',
        maskImage: 'repeating-linear-gradient(to bottom, black 0px, black 20px, transparent 20px, transparent 40px)',
      }}
    />
  );
}

function TimelineStep({ feature, index }: { feature: any, index: number }) {
  const isRight = index % 2 !== 0;

  const cardContent = (side: "left" | "right") => (
    <div
      className="relative w-full max-w-[500px] h-[280px] bg-[#12121A]/80 backdrop-blur-xl border-2 rounded-[20px] p-6 flex flex-row gap-5 transition-all duration-300 hover:-translate-y-2 group shadow-2xl overflow-hidden items-center"
      style={{
        flexDirection: side === "left" ? "row-reverse" : "row",
        borderColor: feature.color,
        boxShadow: `0 0 30px ${feature.color}30, 0 0 60px ${feature.color}15`,
      }}
    >
      <div
        className="absolute inset-0 rounded-[20px] opacity-0 group-hover:opacity-100 transition-opacity duration-500 pointer-events-none"
        style={{ boxShadow: `inset 0 0 60px ${feature.color}15, 0 0 40px ${feature.color}30` }}
      />

      <div className="relative w-[150px] h-[250px] shrink-0 flex items-center justify-center">
        <img
          src={feature.img}
          alt={feature.title}
          className="h-full w-auto max-w-full object-contain rounded-xl"
          style={{ filter: `drop-shadow(0 0 15px ${feature.color}40)` }}
        />
      </div>

      <div className="flex flex-col text-left relative z-10 min-w-0">
        <h3 className="text-3xl font-bold text-white mb-2">{feature.title}</h3>
        <p className="text-base text-gray-400 leading-relaxed">{feature.desc}</p>
      </div>
    </div>
  );

  return (
    <div className="relative w-full flex justify-center mb-20 md:mb-6 last:mb-0">

      {/* Mobile Layout */}
      <div className="md:hidden flex flex-col items-center w-full relative z-10">
        <div
          className="w-9 h-9 rounded-full bg-[#0B0B13] border-[4px] flex items-center justify-center text-white font-bold text-sm mb-5 shadow-xl"
          style={{ borderColor: feature.color, boxShadow: `0 0 20px ${feature.color}80` }}
        >
          {index}
        </div>
        {cardContent("right")}
      </div>

      {/* Desktop Layout */}
      <div className="hidden md:flex w-full relative items-center">

        {/* Center Node */}
        <div
          className="absolute left-1/2 -translate-x-1/2 w-9 h-9 rounded-full bg-[#0B0B13] border-[4px] flex items-center justify-center text-white font-bold text-sm z-20"
          style={{
            borderColor: feature.color,
            boxShadow: `0 0 20px ${feature.color}80, inset 0 0 8px ${feature.color}40`,
          }}
        >
          {index}
        </div>

        {/* Left Side Container */}
        <div className="w-1/2 flex justify-end pr-12 relative">
          {!isRight && (
            <>
              <div
                className="absolute top-1/2 right-0 h-[2px] -translate-y-1/2 z-10"
                style={{ width: "48px", background: `linear-gradient(to left, ${feature.color}, transparent)` }}
              />
              <motion.div
                initial={{ opacity: 0, x: -50, filter: "blur(8px)" }}
                whileInView={{ opacity: 1, x: 0, filter: "blur(0px)" }}
                viewport={{ once: false, margin: "-100px" }}
                transition={{ duration: 0.7, ease: [0.22, 1, 0.36, 1] }}
              >
                {cardContent("left")}
              </motion.div>
            </>
          )}
        </div>

        {/* Right Side Container */}
        <div className="w-1/2 flex justify-start pl-12 relative">
          {isRight && (
            <>
              <div
                className="absolute top-1/2 left-0 h-[2px] -translate-y-1/2 z-10"
                style={{ width: "48px", background: `linear-gradient(to right, ${feature.color}, transparent)` }}
              />
              <motion.div
                initial={{ opacity: 0, x: 50, filter: "blur(8px)" }}
                whileInView={{ opacity: 1, x: 0, filter: "blur(0px)" }}
                viewport={{ once: false, margin: "-100px" }}
                transition={{ duration: 0.7, ease: [0.22, 1, 0.36, 1] }}
              >
                {cardContent("right")}
              </motion.div>
            </>
          )}
        </div>

      </div>
    </div>
  );
}

/* ============================================================
   MAIN COMPONENT
   ============================================================ */
export default function CinematicHero() {
  const navigate = useNavigate();

  // ----- HERO STATE & REFS -----
  const containerRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const progressRef = useRef(0);
  const rafRef = useRef(0);

  const framesRef = useRef<HTMLImageElement[]>([]);
  const finalFrameRef = useRef<HTMLImageElement | null>(null);

  const [scrollPhase, setScrollPhase] = useState(0);
  const [framesReady, setFramesReady] = useState(false);
  const [heroOpacity, setHeroOpacity] = useState(1);
  const [dashOpacity, setDashOpacity] = useState(0);
  const [finalFrameOpacity, setFinalFrameOpacity] = useState(1);
  const [showVideo, setShowVideo] = useState(false);
  const [videoOpacity, setVideoOpacity] = useState(0);
  const showVideoRef = useRef(false);
  const videoRef = useRef<HTMLVideoElement>(null);
  const navigatingRef = useRef(false);
  const boostedRef = useRef(false);
  const scrollModeRef = useRef(false);
  const preloadStayRef = useRef<HTMLVideoElement | null>(null);
  const videoOverlayRef = useRef<HTMLDivElement>(null);
  const [videoSrc, setVideoSrc] = useState("/First-video-main-4k_smooth4x_spedup.mp4");
  const hasFadedVideoInRef = useRef(false);
  const isSecondVideoReadyRef = useRef(false);

  // ----- PRELOAD FRAMES -----
  useEffect(() => {
    let loaded = 0;

    const checkReady = () => {
      if (loaded >= FRAMES_COUNT) setFramesReady(true);
    };

    const finalImg = new Image();
    finalImg.src = "/home-v3_enhanced_x4.png";
    finalFrameRef.current = finalImg;

    const arr: HTMLImageElement[] = [];
    for (let i = 1; i <= FRAMES_COUNT; i++) {
      const img = new Image();
      img.onload = () => {
        loaded++;
        checkReady();
      };
      img.onerror = () => {
        loaded++;
        checkReady();
      };
      img.src = framesSrc(i);
      arr.push(img);
    }
    framesRef.current = arr;

    const timeout = setTimeout(() => setFramesReady(true), 30000);
    return () => clearTimeout(timeout);
  }, []);

  // ----- DRAW CANVAS -----
  const draw = useCallback((progress: number) => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    const w = canvas.width;
    const h = canvas.height;
    if (w === 0 || h === 0) return;

    ctx.clearRect(0, 0, w, h);

    const idx = Math.min(
      Math.floor(progress * FRAMES_COUNT),
      FRAMES_COUNT - 1
    );
    const source = framesRef.current[idx];
    if (!source) return;

    const sW = source.naturalWidth;
    const sH = source.naturalHeight;
    if (sW === 0 || sH === 0) return;

    const srcAspect = sW / sH;
    const canvasAspect = w / h;

    let sx: number, sy: number, sw: number, sh: number;

    if (srcAspect > canvasAspect) {
      sh = sH;
      sw = sh * canvasAspect;
      sx = (sW - sw) / 2;
      sy = 0;
    } else {
      sw = sW;
      sh = sw / canvasAspect;
      sx = 0;
      sy = (sH - sh) / 2;
    }

    ctx.drawImage(source, sx, sy, sw, sh, 0, 0, w, h);
  }, []);

  const lenisRef = useRef<any>(null);

  // ----- SCROLL LOGIC -----
  useLenis(
    useCallback(
      (l: any) => {
        lenisRef.current = l;

        const totalCinH = window.innerHeight * 5.7;
        const p = Math.min(1, Math.max(0, l.scroll / totalCinH));
        progressRef.current = p;
        setScrollPhase(p);

        setHeroOpacity(Math.max(0, 1 - p / 0.12));
        setDashOpacity(p >= 0.88 && !showVideoRef.current ? 1 : 0);
        setFinalFrameOpacity(
          Math.min(1, Math.max(0, (p - 0.94) / 0.06))
        );

        cancelAnimationFrame(rafRef.current);
        rafRef.current = requestAnimationFrame(() => draw(p));
      },
      [draw]
    )
  );

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const resize = () => {
      const w = document.documentElement.clientWidth;
      const h = window.innerHeight;

      canvas.width = w;
      canvas.height = h;
      canvas.style.width = "";
      canvas.style.height = "";

      const ctx = canvas.getContext("2d");
      if (ctx) ctx.setTransform(1, 0, 0, 1, 0, 0);
      draw(progressRef.current);
    };

    resize();
    window.addEventListener("resize", resize);
    return () => {
      window.removeEventListener("resize", resize);
      cancelAnimationFrame(rafRef.current);
    };
  }, [draw]);

  useEffect(() => {
    if (framesReady) draw(progressRef.current);
  }, [framesReady, draw]);

  useEffect(() => {
    const stay = document.createElement("video");
    stay.muted = true;
    stay.preload = "auto";
    stay.src = "/wheretoStay-enhance-2.mp4";
    stay.load();
    preloadStayRef.current = stay;
  }, []);

  // ----- EVENT HANDLERS -----
  const handleScrollToEnd = () => {
    scrollModeRef.current = true;
    showVideoRef.current = true;
    setShowVideo(true);
    hasFadedVideoInRef.current = false;
    if (lenisRef.current) lenisRef.current.stop();
    if (videoOverlayRef.current)
      videoOverlayRef.current.style.transition = "none";
    if (videoRef.current) {
      videoRef.current.currentTime = 0;
      videoRef.current.playbackRate = 0.85;
      videoRef.current.play().catch(() => {});
    }
  };

  const handleContinue = () => {
    scrollModeRef.current = false;
    showVideoRef.current = true;
    setShowVideo(true);
    if (lenisRef.current) lenisRef.current.stop();
    boostedRef.current = false;
    setVideoOpacity(1);
    setVideoSrc("/wheretoStay-enhance-2.mp4");
    isSecondVideoReadyRef.current = true;
  };

  const handleTimeUpdate = useCallback(() => {
    const v = videoRef.current;
    if (!v || navigatingRef.current) return;
    const t = v.currentTime;

    if (scrollModeRef.current) {
      if (t > 0.05 && !hasFadedVideoInRef.current) {
        hasFadedVideoInRef.current = true;
        setVideoOpacity(1);
      }
      const dur = v.duration;
      if (dur && t >= dur - 0.3) setHeroOpacity(0);
      return;
    }

    if (!boostedRef.current && t > 0 && t <= 1) v.playbackRate = 2;
    if (!boostedRef.current && t > 1) {
      boostedRef.current = true;
      v.playbackRate = 1.3;
    }
    const dur = v.duration;
    if (dur && t >= dur) {
      navigatingRef.current = true;
      navigate("/where-to-stay");
    }
  }, [navigate]);

  const handleVideoEnd = () => {
    if (scrollModeRef.current) {
      scrollModeRef.current = false;
      showVideoRef.current = false;
      setHeroOpacity(0);
      setDashOpacity(1);
      setFinalFrameOpacity(1);
      if (videoOverlayRef.current) {
        videoOverlayRef.current.style.transition = "opacity 700ms";
        videoOverlayRef.current.getBoundingClientRect();
      }
      setVideoOpacity(0);
      setTimeout(() => {
        if (videoOverlayRef.current)
          videoOverlayRef.current.style.transition = "";
        setShowVideo(false);
        progressRef.current = 1;
        draw(1);
        if (lenisRef.current) {
          lenisRef.current.start();
          lenisRef.current.scrollTo(window.innerHeight * 5.7, {
            immediate: true,
            force: true,
          });
        }
      }, 700);
    } else if (!navigatingRef.current) {
      navigatingRef.current = true;
      navigate("/where-to-stay");
    }
  };

  const handleVideoLoad = useCallback(() => {
    const v = videoRef.current;
    if (!v || !isSecondVideoReadyRef.current) return;
    isSecondVideoReadyRef.current = false;
    v.playbackRate = 2;
    v.play().catch(() => {});
  }, []);

  /* ==========================================================
     RENDER
     ========================================================== */
  return (
    <Lenis root>
      <div className="bg-[#0B0B13] min-h-screen">
        {/* ---- Loading Screen ---- */}
        {!framesReady && (
          <div className="fixed inset-0 flex items-center justify-center bg-[#0B0B13] z-50">
            <div className="flex flex-col items-center gap-4">
              <div className="w-14 h-14 rounded-xl bg-primary/20 border border-primary/30 flex items-center justify-center animate-pulse-glow">
                <Sparkles className="w-7 h-7 text-purple-400" />
              </div>
              <p className="text-sm text-gray-400">Preparing experience...</p>
            </div>
          </div>
        )}

        {/* ===================================================
            CINEMATIC HERO SECTION (Scroll-driven canvas)
            =================================================== */}
        <div ref={containerRef} className="relative" style={{ height: "685vh" }}>
          <div className="sticky top-0 w-full h-screen overflow-hidden">
            {/* Canvas for frame sequence */}
            <canvas
              ref={canvasRef}
              className="absolute inset-0 w-full h-full"
              style={{
                display: showVideo && videoOpacity >= 1 ? "none" : "block",
              }}
            />

            {/* Final frame image overlay */}
            <img
              src="/home-v3_enhanced_x4.png"
              className="absolute inset-0 w-full h-full object-cover pointer-events-none transition-opacity duration-700"
              style={{ opacity: finalFrameOpacity }}
              alt="Final Frame"
            />

            {/* Gradient overlay */}
            <div className="absolute inset-0 bg-gradient-to-b from-[#0B0B13]/40 via-transparent to-[#0B0B13] pointer-events-none z-[1]" />

            {/* ---- NAVBAR ON FIRST FRAME ---- */}
            <div
              className="absolute top-0 left-0 w-full z-20 pointer-events-auto transition-opacity duration-300"
              style={{ opacity: heroOpacity }}
            >
              <Navbar />
            </div>

            {/* ---- HERO TITLE LAYER ---- */}
            <div
              className="absolute inset-0 z-10 flex flex-col items-center justify-center text-center px-4 pointer-events-none transition-opacity duration-100"
              style={{ opacity: heroOpacity }}
            >
              <h1 className="text-5xl md:text-6xl lg:text-7xl text-white mb-4 tracking-tight">
                <span className="font-extralight block mb-1">
                  Explore Egypt with
                </span>
                <span className="font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-[#FACC15] to-[#D4AF37]">
                  Local Intelligence
                </span>
              </h1>
              <p className="text-white text-base md:text-lg font-medium tracking-widest uppercase drop-shadow-[0_2px_4px_rgba(0,0,0,0.8)] max-w-xl mx-auto mb-8">
                AI-powered itineraries, crafted by locals
              </p>

              {/* Scroll Down Button */}
              <div className="flex flex-col items-center gap-2 pointer-events-auto">
                <button
                  onClick={handleScrollToEnd}
                  className="bg-[#DDA73B] hover:bg-[#EAB308] text-black font-bold px-6 py-3 rounded-full text-sm transition-all hover:scale-105"
                >
                  Scroll Down
                </button>
                <div className="flex flex-col items-center gap-0.5">
                  <ChevronDown
                    strokeWidth={3}
                    className="w-6 h-6 text-[#DDA73B] animate-pulse-glow"
                    style={{ animationDelay: '0s' }}
                  />
                  <ChevronDown
                    strokeWidth={3}
                    className="w-6 h-6 text-[#DDA73B] animate-pulse-glow"
                    style={{ animationDelay: '0.4s' }}
                  />
                  <ChevronDown
                    strokeWidth={3}
                    className="w-6 h-6 text-[#DDA73B] animate-pulse-glow"
                    style={{ animationDelay: '0.8s' }}
                  />
                </div>
              </div>
            </div>

            {/* ---- DASHBOARD LAYER (appears at scroll end) ---- */}
            <div
              className="absolute inset-0 z-10 pointer-events-none transition-opacity duration-700 flex flex-col justify-start items-center overflow-y-auto pt-52 max-md:pt-24"
              style={{ opacity: dashOpacity }}
            >
              {/* Inline Navbar (solid mode) */}
              <div
                className="w-full absolute top-0 left-0 z-20"
                style={{
                  pointerEvents: dashOpacity > 0.5 ? "auto" : "none",
                }}
              >
                <Navbar solid />
              </div>

              <div
                className="w-full max-w-[95%] xl:max-w-[1200px] mx-auto flex flex-col items-center px-4"
                style={{
                  pointerEvents: dashOpacity > 0.5 ? "auto" : "none",
                }}
              >
                {/* Title */}
                <div className="text-center mb-6 md:mb-8">
                  <h1 className="text-2xl md:text-[32px] leading-tight text-white mb-1">
                    <span className="font-light text-xl md:text-[28px]">
                      Explore Egypt with
                    </span>
                    <br />
                    <span className="font-bold text-3xl md:text-[42px] text-[#DDA73B] drop-shadow-[0_2px_8px_rgba(0,0,0,0.9)] [text-shadow:0_2px_0_rgb(0,0,0),0_4px_8px_rgba(0,0,0,0.8)]">
                      Local Intelligence
                    </span>
                  </h1>
                  <p className="text-white text-xs md:text-sm drop-shadow-[0_1px_2px_rgba(0,0,0,0.8)]">
                    AI-powered itineraries, crafted by locals
                  </p>
                </div>

                {/* Search Bar */}
                <div className="w-full max-w-[650px] max-md:max-w-full mb-5 md:mb-6">
                  <div className="bg-white rounded-full p-1 flex items-center shadow-lg">
                    <Search className="w-4 h-4 text-purple-600 ml-3 shrink-0" />
                    <input
                      placeholder="Where do you want to go?"
                      className="flex-1 bg-transparent text-gray-800 placeholder:text-gray-400 focus:outline-none px-3 text-sm"
                      onKeyDown={(e) => {
                        if (
                          e.key === "Enter" &&
                          (e.target as HTMLInputElement).value.trim()
                        ) {
                          navigate(
                            `/where-to-stay?city=${encodeURIComponent(
                              (e.target as HTMLInputElement).value.trim()
                            )}`
                          );
                        }
                      }}
                    />
                    <button
                      onClick={handleScrollToEnd}
                      className="bg-[#8B5CF6] hover:bg-[#7C3AED] text-white rounded-full py-2 px-4 flex items-center gap-1.5 text-xs font-medium transition-all"
                    >
                      <Sparkles className="w-3 h-3" />
                      AI Plan My Trip
                    </button>
                    <button
                      onClick={handleContinue}
                      className="ml-2 bg-transparent border border-[#DDA73B] text-[#DDA73B] hover:bg-[#DDA73B]/10 rounded-full py-2 px-4 flex items-center gap-1.5 text-xs font-medium transition-all"
                    >
                      Where to Stay
                      <ArrowRight className="w-3 h-3" />
                    </button>
                  </div>
                </div>

                {/* Vibe Filters */}
                <div className="flex flex-wrap justify-center gap-2 mb-5 md:mb-6">
                  <button className="bg-transparent border border-[#8B5CF6] text-[#8B5CF6] px-3 py-1 rounded-full text-xs font-medium flex items-center gap-1.5">
                    <Sparkles className="w-3 h-3" /> All Vibes
                  </button>
                  {[
                    { label: "Relaxing", icon: <Leaf className="w-3 h-3" /> },
                    {
                      label: "Cultural",
                      icon: <Landmark className="w-3 h-3" />,
                    },
                    {
                      label: "Adventurous",
                      icon: <Mountain className="w-3 h-3" />,
                    },
                    { label: "Family", icon: <Users className="w-3 h-3" /> },
                    {
                      label: "Luxury",
                      icon: <Diamond className="w-3 h-3" />,
                    },
                  ].map((v) => (
                    <button
                      key={v.label}
                      onClick={() =>
                        navigate(
                          `/where-to-go?vibe=${encodeURIComponent(v.label)}`
                        )
                      }
                      className="bg-black/50 border border-white/20 hover:border-white/40 text-white px-3 py-1 rounded-full text-xs font-medium flex items-center gap-1.5 backdrop-blur-md transition-all"
                    >
                      <span className="text-gray-300">{v.icon}</span>{" "}
                      {v.label}
                    </button>
                  ))}
                </div>

                {/* Trending Now */}
                <div className="w-full relative">
                  <div className="flex items-center justify-between mb-2 px-1">
                    <h2 className="text-sm font-medium text-white">
                      Trending Now
                    </h2>
                    <button
                      onClick={() => navigate("/where-to-stay")}
                      className="text-purple-400 text-xs font-medium flex items-center gap-1 hover:text-purple-300 transition-colors"
                    >
                      View all <ChevronRight className="w-3 h-3" />
                    </button>
                  </div>
                  <div className="grid grid-cols-5 max-md:grid-cols-2 max-sm:grid-cols-1 gap-2">
                    {[
                      {
                        name: "Pyramids of Giza",
                        cat: "CULTURAL",
                        rating: "4.9",
                        rev: "2.5k",
                        img: luxorImg,
                      },
                      {
                        name: "Nile Felucca Ride",
                        cat: "RELAXING",
                        rating: "4.8",
                        rev: "1.8k",
                        img: cairoImg,
                      },
                      {
                        name: "White Desert Safari",
                        cat: "ADVENTUROUS",
                        rating: "4.9",
                        rev: "950",
                        img: aswanImg,
                      },
                      {
                        name: "Karnak Temple",
                        cat: "CULTURAL",
                        rating: "4.8",
                        rev: "1.2k",
                        img: alexandriaImg,
                      },
                      {
                        name: "Siwa Oasis Retreat",
                        cat: "LUXURY",
                        rating: "4.9",
                        rev: "630",
                        img: luxorImg,
                      },
                    ].map((item) => (
                      <div
                        key={item.name}
                        onClick={() =>
                          navigate(`/where-to-stay?city=${item.name}`)
                        }
                        className="relative rounded-[12px] overflow-hidden group cursor-pointer border border-white/10 shadow-xl"
                        style={{ height: "110px" }}
                      >
                        <img
                          src={item.img}
                          className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
                          alt={item.name}
                        />
                        <div className="absolute inset-0 bg-gradient-to-t from-black/90 via-black/30 to-transparent" />
                        <div className="absolute bottom-0 left-0 p-2 w-full">
                          <p className="text-[8px] font-bold text-gray-300 tracking-wider mb-0.5 uppercase">
                            {item.cat}
                          </p>
                          <h3 className="text-[11px] font-semibold text-white mb-0.5 leading-tight">
                            {item.name}
                          </h3>
                          <div className="flex items-center gap-1 text-[10px] font-medium">
                            <span className="flex items-center gap-0.5 text-[#EAB308]">
                              <Star className="w-2.5 h-2.5 fill-current" />{" "}
                              {item.rating}
                            </span>
                            <span className="text-gray-400">
                              ({item.rev})
                            </span>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>

            {/* Where to Stay floating button */}
            {dashOpacity > 0.5 && (
              <div className="absolute bottom-8 right-8 max-md:left-1/2 max-md:-translate-x-1/2 max-md:bottom-4 z-20">
                <button
                  onClick={handleContinue}
                  className="bg-gradient-to-r from-[#DDA73B] to-[#EAB308] text-black font-bold px-5 py-3 rounded-full flex items-center gap-2 shadow-lg hover:scale-105 transition-transform cursor-pointer"
                >
                  Where to Stay <ArrowRight className="w-4 h-4" />
                </button>
              </div>
            )}

            {/* Overlay 1 — airplane window phase: p 0.13 → 0.28 */}
            {/* Starts AFTER heroOpacity fully fades at p=0.12 */}
            <div
              className="absolute inset-0 z-10 flex flex-col items-center justify-center pointer-events-none"
              style={{
                opacity: (() => {
                  const p = scrollPhase;
                  if (p < 0.13 || p > 0.28) return 0;
                  if (p < 0.19) return (p - 0.13) / 0.06;   // fade in over 6%
                  return 1 - (p - 0.19) / 0.09;              // fade out over 9%
                })()
              }}
            >
              <p className="text-white/60 text-sm uppercase tracking-[0.3em] mb-3 font-light drop-shadow-[0_2px_8px_rgba(0,0,0,0.9)]">
                Your journey begins
              </p>
              <h2 className="text-white text-4xl md:text-6xl font-bold text-center drop-shadow-[0_4px_24px_rgba(0,0,0,0.8)]">
                Egypt is<br />
                <span className="text-[#FACC15]">Waiting for You</span>
              </h2>
            </div>

            {/* Overlay 2 — above clouds phase: p 0.38 → 0.62 */}
            <div
              className="absolute inset-0 z-10 flex flex-col items-start justify-end pb-24 pl-8 md:pl-16 pointer-events-none"
              style={{
                opacity: (() => {
                  const p = scrollPhase;
                  if (p < 0.38 || p > 0.62) return 0;
                  if (p < 0.50) return (p - 0.38) / 0.12;   // fade in
                  return 1 - (p - 0.50) / 0.12;              // fade out
                })()
              }}
            >
              <p className="text-white/40 text-xs uppercase tracking-[0.5em] mb-4 font-light" style={{ textShadow: '0 2px 0 rgba(0,0,0,0.8), 0 4px 20px rgba(0,0,0,0.9), 0 0 60px rgba(0,0,0,0.6)' }}>
                5,000 years of wonder
              </p>
              <h2 className="text-6xl md:text-8xl font-extrabold text-left" style={{ textShadow: '0 2px 0 rgba(0,0,0,0.8), 0 4px 20px rgba(0,0,0,0.9), 0 0 60px rgba(0,0,0,0.6)' }}>
                <span className="text-white/90">Beneath these clouds,</span><br />
                <span className="text-[#FACC15]">history awaits</span>
              </h2>
            </div>

            {/* Overlay 3 — pyramid reveal phase: p 0.68 → 0.86 */}
            {/* Hard cutoff at 0.86 — dashboard appears at 0.88 */}
            <div
              className="absolute inset-0 z-10 flex flex-col items-center justify-center pointer-events-none"
              style={{
                opacity: (() => {
                  const p = scrollPhase;
                  if (p < 0.68 || p > 0.86) return 0;
                  if (p < 0.76) return (p - 0.68) / 0.08;   // fade in
                  return 1 - (p - 0.76) / 0.10;              // fade out, done by 0.86
                })()
              }}
            >
              <div className="relative px-12 py-10">
                <div className="absolute inset-0 bg-gradient-radial from-black/50 via-black/30 to-transparent rounded-3xl" />
                <div className="relative z-10 flex flex-col items-center">
                  <p className="text-[#FACC15] text-[10px] uppercase tracking-[0.6em] mb-4 font-semibold" style={{ textShadow: '0 2px 0 rgba(0,0,0,0.8), 0 4px 20px rgba(0,0,0,0.9), 0 0 60px rgba(0,0,0,0.6)' }}>
                    Giza, Egypt
                  </p>
                  <h2 className="text-center leading-none" style={{ textShadow: '0 2px 0 rgba(0,0,0,0.8), 0 4px 20px rgba(0,0,0,0.9), 0 0 60px rgba(0,0,0,0.6)' }}>
                    <span className="text-white font-light text-5xl md:text-7xl">Welcome to</span><br />
                    <span className="text-[#FACC15] font-black text-8xl md:text-[120px]">Egypt</span>
                  </h2>
                </div>
              </div>
            </div>

            {/* Video overlay */}
            <div
              ref={videoOverlayRef}
              className="absolute inset-0 z-30 pointer-events-none"
              style={{ opacity: videoOpacity }}
            >
              <video
                ref={videoRef}
                src={videoSrc}
                className="w-full h-full object-cover"
                style={{ filter: "saturate(1.4) contrast(1.15) brightness(1.05)" }}
                muted
                playsInline
                preload="auto"
                onTimeUpdate={handleTimeUpdate}
                onLoadedData={handleVideoLoad}
                onEnded={handleVideoEnd}
              />
              <div className="absolute inset-0 bg-black/20" />
            </div>
          </div>
        </div>

        {/* ===================================================
            CONTENT SECTIONS (below the fold)
            =================================================== */}
        <div className="relative z-40 bg-[#0B0B13] overflow-hidden">

          {/* ---- Scroll Down Indicator ---- */}
          <div className="relative z-20 flex items-center justify-center gap-4 py-8">
            <div className="h-px w-16 bg-gradient-to-r from-transparent to-[#DDA73B]/50" />
            <ChevronDown className="w-4 h-4 text-[#DDA73B]/60" />
            <span className="text-[10px] text-[#DDA73B]/60 uppercase tracking-[0.2em] font-medium">
              Scroll to explore
            </span>
            <ChevronDown className="w-4 h-4 text-[#DDA73B]/60" />
            <div className="h-px w-16 bg-gradient-to-l from-transparent to-[#DDA73B]/50" />
          </div>

          {/* ============================================
              SECTION 2 — IMMERSIVE 3D
              "Egypt Like Never Before"
              ============================================ */}
          <section className="relative z-20 py-16 lg:py-24 px-6 max-w-[1200px] mx-auto">
            {/* Subtle constellation bg overlay */}
            <div className="absolute inset-0 pointer-events-none overflow-hidden">
              <img
                src={bgConstellations}
                alt=""
                className="w-full h-full object-cover opacity-30 scale-110"
                style={{ filter: "hue-rotate(260deg) saturate(1.5)" }}
              />
              <div className="absolute inset-0 bg-gradient-to-b from-[#0B0B13] via-transparent to-[#0B0B13]" />
            </div>

            <div className="relative z-10 flex flex-col lg:flex-row items-center gap-10 lg:gap-16">
              {/* Left: Text */}
              <div className="flex-1 text-center lg:text-left">
                <div className="inline-flex items-center gap-2 bg-[#1A1128] text-[#D8B4FE] px-4 py-1.5 rounded-full text-xs font-semibold border border-[#9333EA]/30 mb-6 shadow-lg">
                  <Eye className="w-3.5 h-3.5" /> Immersive 3D
                </div>

                <h2 className="text-4xl md:text-5xl lg:text-[56px] font-bold text-white leading-[1.05] mb-4 tracking-tight">
                  Egypt Like
                  <br />
                  <span className="text-[#FACC15]">Never Before</span>
                </h2>

                <p className="text-gray-400 text-sm md:text-[15px] max-w-[380px] mx-auto lg:mx-0 leading-relaxed mb-2">
                  Explore ancient wonders through cutting-edge 3D technology.
                </p>
                <p className="text-gray-500 text-sm md:text-[15px] max-w-[380px] mx-auto lg:mx-0 leading-relaxed mb-8">
                  Every monument, every story, brought to life.
                </p>

                <button className="inline-flex items-center gap-3 text-white text-sm font-semibold hover:text-[#D8B4FE] transition-colors group">
                  <div className="w-10 h-10 rounded-full bg-[#7E22CE] flex items-center justify-center transition-transform group-hover:scale-110 shadow-[0_0_20px_rgba(126,34,206,0.5)]">
                    <ArrowRight className="w-4 h-4 text-white" />
                  </div>
                  Discover in 3D
                </button>
              </div>

              {/* Right: 3D Portal Image */}
              <div className="flex-1 relative w-full flex justify-center items-center">
                {/* Purple glow behind image */}
                <div className="absolute w-[90%] aspect-square bg-[#7E22CE]/25 blur-[120px] rounded-full pointer-events-none" />
                <div className="absolute w-[60%] aspect-square bg-[#A855F7]/20 blur-[80px] rounded-full pointer-events-none" />

                <div className="relative w-full max-w-[600px] z-10">
                  <img
                    src={img3DPortal}
                    alt="3D Egyptian Portal"
                    className="w-full h-auto object-contain drop-shadow-[0_0_60px_rgba(168,85,247,0.35)]"
                  />
                  {/* 360° badge */}
                  <div className="absolute top-4 right-4 bg-[#1A1128]/80 backdrop-blur-md border border-[#9333EA]/30 text-[#D8B4FE] px-3 py-1 rounded-full text-xs font-semibold flex items-center gap-1.5 shadow-lg">
                    <Compass className="w-3.5 h-3.5" /> 360&deg;
                  </div>
                </div>
              </div>
            </div>
          </section>

          {/* ============================================
              SECTION 3 — THE JOURNEY TIMELINE
              ============================================ */}
          <section className="relative z-20 py-24 md:py-32 px-6 overflow-hidden">
            {/* Background Particles */}
            <div className="absolute inset-0 pointer-events-none">
              {[...Array(20)].map((_, i) => (
                <motion.div
                  key={i}
                  className="absolute bg-white/20 rounded-full"
                  style={{
                    width: Math.random() * 4 + 2 + "px",
                    height: Math.random() * 4 + 2 + "px",
                    left: Math.random() * 100 + "%",
                    top: Math.random() * 100 + "%",
                  }}
                  animate={{
                    y: [0, -100],
                    opacity: [0, 0.3, 0],
                  }}
                  transition={{
                    duration: Math.random() * 10 + 15,
                    repeat: Infinity,
                    ease: "linear",
                  }}
                />
              ))}
            </div>

            {/* Section Header */}
            <div className="relative z-10 text-center mb-16">
              <div className="inline-flex items-center bg-[#1A1128] text-[#D8B4FE] px-4 py-1.5 rounded-full text-xs font-semibold border border-[#9333EA]/30 shadow-lg mb-6">
                Why Glinter
              </div>
              <h2 className="text-4xl md:text-6xl font-bold text-white tracking-tight leading-tight">
                Your Journey, <span className="text-[#A855F7]">Intelligently</span> <span className="text-[#FACC15]">Guided</span>
              </h2>
              <p className="text-gray-400 text-sm md:text-lg max-w-2xl mx-auto mt-4">
                Every feature built with one mission: making your Egyptian adventure seamless, safe, and unforgettable.
              </p>
            </div>

            <div className="max-w-[1200px] mx-auto relative">
              {/* Central Timeline Line — dashed gradient */}
              <div className="absolute left-1/2 -translate-x-1/2 top-0 bottom-0 hidden md:block">
                <TimelineProgressLine />
              </div>

              {/* Start Point: Lotus Crystal — larger with glow */}
              <div className="relative z-10 flex justify-center mb-20">
                 <div className="relative">
                    <div className="absolute inset-0 bg-purple-500/50 blur-[60px] animate-pulse" />
                    <div className="absolute inset-0 bg-purple-400/30 blur-[40px] animate-pulse" style={{ animationDelay: "0.5s" }} />
                    <img src={imgCard1} alt="Start" className="w-48 h-48 object-contain relative z-10 drop-shadow-[0_0_40px_rgba(168,85,247,0.6)]" />
                 </div>
              </div>

              {/* Features List — tighter spacing */}
              <div className="space-y-10 md:space-y-14">
                {journeyFeatures.map((feature, idx) => (
                  <TimelineStep key={idx} feature={feature} index={idx + 1} />
                ))}
              </div>

              {/* Bottom Emblem — Egyptian scarab */}
              <div className="relative z-10 flex justify-center mt-20">
                 <div className="relative">
                    <div className="absolute inset-0 bg-orange-500/40 blur-[50px] animate-pulse" />
                    <div className="absolute inset-0 bg-yellow-400/25 blur-[35px] animate-pulse" style={{ animationDelay: "0.3s" }} />
                    <svg viewBox="0 0 120 80" className="w-32 h-20 relative z-10 drop-shadow-[0_0_30px_rgba(249,115,22,0.6)]">
                      {/* Winged scarab silhouette */}
                      <defs>
                        <linearGradient id="scarabGrad" x1="0%" y1="0%" x2="100%" y2="100%">
                          <stop offset="0%" stopColor="#F97316" />
                          <stop offset="50%" stopColor="#FACC15" />
                          <stop offset="100%" stopColor="#F97316" />
                        </linearGradient>
                      </defs>
                      {/* Body */}
                      <ellipse cx="60" cy="45" rx="12" ry="15" fill="url(#scarabGrad)" opacity="0.9" />
                      {/* Head */}
                      <circle cx="60" cy="28" r="7" fill="url(#scarabGrad)" opacity="0.9" />
                      {/* Left wing */}
                      <path d="M48 40 Q20 25 10 40 Q20 55 48 50 Z" fill="url(#scarabGrad)" opacity="0.7" />
                      {/* Right wing */}
                      <path d="M72 40 Q100 25 110 40 Q100 55 72 50 Z" fill="url(#scarabGrad)" opacity="0.7" />
                      {/* Sun disk */}
                      <circle cx="60" cy="15" r="10" fill="none" stroke="url(#scarabGrad)" strokeWidth="1.5" opacity="0.6" />
                      <circle cx="60" cy="15" r="6" fill="#FACC15" opacity="0.4" />
                    </svg>
                 </div>
              </div>
            </div>
          </section>

          {/* ============================================
              SECTION 4 — EGYPT IN NUMBERS (Stats)
              ============================================ */}
          <section className="relative z-20 py-24 lg:py-32 px-6 overflow-hidden">
            {/* Background image: night pyramids + network lines */}
            <div className="absolute inset-0 z-0 pointer-events-none">
              <img
                src={bgStats}
                alt=""
                className="w-full h-full object-cover"
                style={{ filter: "brightness(0.5) saturate(1.2)" }}
              />
              <div className="absolute inset-0 bg-gradient-to-b from-[#0B0B13]/70 via-[#0B0B13]/40 to-[#0B0B13]/70" />
            </div>

            <div className="relative z-10 max-w-[900px] mx-auto">
              <h2 className="text-center text-3xl md:text-4xl font-bold text-white mb-14">
                Egypt in <span className="text-[#FACC15]">Numbers</span>
              </h2>

              <div className="grid grid-cols-2 md:grid-cols-4 gap-5">
                {[
                  {
                    icon: <BookOpen className="text-[#FACC15] w-5 h-5" />,
                    val: "5000+",
                    lbl: "Years of History",
                  },
                  {
                    icon: <Star className="text-[#FACC15] w-5 h-5" />,
                    val: "100+",
                    lbl: "Curated Experiences",
                  },
                  {
                    icon: <Users className="text-[#FACC15] w-5 h-5" />,
                    val: "50+",
                    lbl: "Local Guides",
                  },
                  {
                    icon: <Award className="text-[#FACC15] w-5 h-5" />,
                    val: "4.9",
                    lbl: "Average Rating",
                  },
                ].map((stat, i) => (
                  <div
                    key={i}
                    className="bg-[#12121A]/70 backdrop-blur-lg border border-white/[0.06] border-t-[2.5px] border-t-[#FACC15]/40 rounded-2xl p-7 flex flex-col items-center justify-center relative overflow-hidden transition-all hover:bg-[#161622] hover:border-white/10"
                  >
                    {/* Top glow */}
                    <div className="absolute top-0 w-full h-12 bg-gradient-to-b from-[#FACC15]/8 to-transparent pointer-events-none" />
                    <div className="w-11 h-11 rounded-full flex items-center justify-center bg-[#FACC15]/10 border border-[#FACC15]/20 mb-4 z-10">
                      {stat.icon}
                    </div>
                    <div className="text-[28px] md:text-[32px] font-bold text-white mb-1.5 z-10">
                      {stat.val}
                    </div>
                    <div className="text-gray-400 text-[11px] uppercase tracking-[0.08em] font-medium text-center z-10">
                      {stat.lbl}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </section>

          {/* ============================================
              SECTION 5 — CTA
              "Your Egyptian Adventure Awaits"
              ============================================ */}
          <section className="relative z-20 py-20 lg:py-28 px-6 max-w-[1100px] mx-auto mb-10">
            <div className="bg-[#12121A]/60 backdrop-blur-xl border border-white/[0.07] rounded-[28px] overflow-hidden shadow-[0_0_60px_rgba(168,85,247,0.12)] flex flex-col lg:flex-row items-stretch">
              {/* Left — Portal Panorama Image */}
              <div className="relative w-full lg:w-[48%] min-h-[280px] lg:min-h-[380px] flex items-center justify-center overflow-hidden p-6">
                <div className="absolute w-[85%] aspect-square bg-[#7E22CE]/18 blur-[100px] rounded-full pointer-events-none" />
                <img
                  src={imgCTAPortal}
                  alt="Egypt Adventure"
                  className="relative z-10 w-full h-full object-contain drop-shadow-[0_0_40px_rgba(168,85,247,0.3)]"
                />
              </div>

              {/* Right — Content */}
              <div className="relative w-full lg:w-[52%] p-8 lg:p-12 flex flex-col justify-center">
                <div className="inline-flex items-center gap-2 bg-[#1A1128] text-[#D8B4FE] px-4 py-1.5 rounded-full text-xs font-semibold border border-[#9333EA]/30 mb-6 w-fit shadow-lg">
                  <Sparkles className="w-3.5 h-3.5" /> Ready for Adventure?
                </div>

                <h2 className="text-3xl md:text-4xl lg:text-[44px] font-bold text-white leading-[1.08] mb-5 tracking-tight">
                  Your Egyptian
                  <br />
                  <span className="text-[#FACC15]">Adventure Awaits</span>
                </h2>

                <p className="text-gray-400 text-sm md:text-[15px] max-w-[420px] leading-relaxed mb-8">
                  Let AI craft your perfect itinerary. Discover hidden gems,
                  book with confidence, and travel like a local.
                </p>

                <div className="flex flex-wrap gap-3">
                  <button className="bg-[#FACC15] hover:bg-[#EAB308] text-black font-bold px-7 py-3 rounded-full flex items-center gap-2 transition-all hover:scale-105 shadow-xl text-sm">
                    Start Planning <ArrowRight className="w-4 h-4" />
                  </button>
                  <button className="border border-white/20 hover:border-white/40 text-white px-7 py-3 rounded-full flex items-center gap-2 transition-all hover:scale-105 font-medium text-sm">
                    <Compass className="w-4 h-4" /> Explore Destinations
                  </button>
                </div>
              </div>
            </div>
          </section>

          {/* ---- Footer ---- */}
          <Footer />
        </div>
      </div>
    </Lenis>
  );
}