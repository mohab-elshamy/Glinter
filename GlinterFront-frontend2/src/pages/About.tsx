import { Users, Heart, MapPin, Compass, CheckCircle, Sparkles, ArrowRight, Globe } from "lucide-react";
import { motion } from "framer-motion";
import { Link } from "react-router-dom";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import aboutHero from "@/assets/about-hero.jpg";

const stats = [
  { icon: Users, value: "50K+", label: "Travelers Helped" },
  { icon: Heart, value: "2,500+", label: "Local Buddies" },
  { icon: MapPin, value: "180+", label: "Destinations" },
  { icon: Sparkles, value: "10K+", label: "Experiences Shared" },
];

const steps = [
  { num: "1", title: "Share Your Vibes", desc: "Tell us your travel preferences and what kind of experience you're looking for" },
  { num: "2", title: "Get AI Recommendations", desc: "Our AI analyzes heatmaps and local insights to suggest perfect spots" },
  { num: "3", title: "Connect with Locals", desc: "Match with verified local buddies who share your interests" },
  { num: "4", title: "Explore Authentically", desc: "Discover hidden gems and create unforgettable memories" },
];

const values = [
  { icon: Heart, title: "Authentic Experiences", desc: "We believe travel should be about genuine connections and local culture, not tourist traps." },
  { icon: Users, title: "Community First", desc: "Our platform empowers local communities by creating economic opportunities." },
  { icon: Sparkles, title: "Smart Technology", desc: "AI-powered recommendations that understand your unique travel style." },
  { icon: Globe, title: "Sustainable Tourism", desc: "Promoting responsible travel that benefits both visitors and destinations." },
];

const About = () => (
  <div className="min-h-screen bg-background">
    <Navbar />

    {/* Hero */}
    <section className="relative overflow-hidden">
      <div className="absolute inset-0">
        <img src={aboutHero} alt="Egypt" className="w-full h-full object-cover opacity-25" />
        <div className="absolute inset-0 bg-gradient-to-b from-background/60 via-background/80 to-background" />
      </div>
      <div className="relative container mx-auto px-4 py-20 text-center">
        <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}>
          <span className="section-badge mb-4 inline-block">✨ AI-Powered Travel Companion</span>
          <h1 className="text-4xl md:text-5xl font-extrabold mb-4">
            <span className="text-gradient-orange">Discover Smarter.</span><br />
            Travel Brighter.
          </h1>
          <p className="text-muted-foreground max-w-lg mx-auto mb-8">
            Glinter transforms how you explore the world by connecting you with local knowledge,
            AI-powered insights, and authentic experiences that match your unique travel vibe.
          </p>
          <div className="flex justify-center gap-3">
            <Link to="/explore" className="btn-accent inline-flex items-center gap-2 text-sm">
              Start Exploring <ArrowRight className="w-4 h-4" />
            </Link>
            <Link to="/local-buddies" className="btn-outline-light text-sm">
              Become a Local Buddy
            </Link>
          </div>
        </motion.div>
      </div>
    </section>

    {/* Stats */}
    <section className="container mx-auto px-4 py-12">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-6 text-center">
        {stats.map((s) => (
          <div key={s.label}>
            <div className="w-10 h-10 rounded-xl bg-primary/15 flex items-center justify-center mx-auto mb-3">
              <s.icon className="w-5 h-5 text-primary" />
            </div>
            <div className="text-2xl font-extrabold text-gradient-purple">{s.value}</div>
            <p className="text-xs text-muted-foreground mt-1">{s.label}</p>
          </div>
        ))}
      </div>
    </section>

    {/* Mission */}
    <section className="container mx-auto px-4 py-16">
      <div className="grid md:grid-cols-2 gap-10 items-center">
        <div>
          <span className="section-badge mb-4 inline-block">Our Mission</span>
          <h2 className="text-2xl font-bold mb-4">
            Making Travel Personal, <span className="text-gradient-orange">Not Generic</span>
          </h2>
          <p className="text-sm text-muted-foreground mb-6">
            We started Glinter because we were tired of cookie-cutter travel recommendations.
            Every traveler is unique, and their experiences should be too. By combining cutting-edge AI
            with local expertise, we're creating a new way to explore the world.
          </p>
          <div className="space-y-3">
            {["AI-powered heatmaps showing real-time crowds", "Verified local buddies in every destination", "Personalized itineraries based on your vibes"].map((item) => (
              <div key={item} className="flex items-center gap-2 text-sm">
                <CheckCircle className="w-4 h-4 text-green-400 shrink-0" />
                {item}
              </div>
            ))}
          </div>
        </div>
        <div className="card-glass p-8 text-center">
          <div className="w-16 h-16 rounded-2xl bg-primary/15 flex items-center justify-center mx-auto mb-4">
            <Compass className="w-8 h-8 text-primary" />
          </div>
          <h3 className="font-bold text-lg mb-2">Explore with Purpose</h3>
          <div className="flex items-center justify-center gap-2 mt-4">
            <div className="flex -space-x-2">
              {[...Array(3)].map((_, i) => (
                <div key={i} className="w-8 h-8 rounded-full bg-accent/30 border-2 border-card" />
              ))}
            </div>
            <span className="text-xs text-muted-foreground">2,500+ Buddies ready to guide you</span>
          </div>
        </div>
      </div>
    </section>

    {/* Steps */}
    <section className="container mx-auto px-4 py-16 text-center">
      <span className="section-badge mb-4 inline-block">How It Works</span>
      <h2 className="text-2xl font-bold mb-10">
        Your Journey, <span className="text-gradient-orange">Simplified</span>
      </h2>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {steps.map((s) => (
          <div key={s.num} className="card-glass p-5 text-left">
            <div className="w-8 h-8 rounded-lg bg-primary/15 text-primary font-bold text-sm flex items-center justify-center mb-3">
              {s.num}
            </div>
            <h3 className="font-bold text-sm mb-1">{s.title}</h3>
            <p className="text-xs text-muted-foreground">{s.desc}</p>
          </div>
        ))}
      </div>
    </section>

    {/* Values */}
    <section className="container mx-auto px-4 py-16 text-center">
      <span className="section-badge mb-4 inline-block">Our Values</span>
      <h2 className="text-2xl font-bold mb-10">
        What We <span className="text-gradient-orange">Stand For</span>
      </h2>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 max-w-2xl mx-auto">
        {values.map((v) => (
          <div key={v.title} className="card-glass p-5 text-left flex gap-3">
            <div className="w-9 h-9 rounded-lg bg-primary/15 flex items-center justify-center shrink-0">
              <v.icon className="w-4 h-4 text-primary" />
            </div>
            <div>
              <h3 className="font-bold text-sm mb-1">{v.title}</h3>
              <p className="text-xs text-muted-foreground">{v.desc}</p>
            </div>
          </div>
        ))}
      </div>
    </section>

    {/* CTA */}
    <section className="container mx-auto px-4 py-16">
      <div className="card-glass p-10 text-center bg-gradient-to-br from-accent/10 to-primary/10">
        <h2 className="text-2xl font-bold mb-3">
          Ready to Explore <span className="text-gradient-orange">Differently?</span>
        </h2>
        <p className="text-sm text-muted-foreground mb-6 max-w-md mx-auto">
          Join thousands of travelers who've discovered the joy of authentic, AI-powered exploration with Glinter.
        </p>
        <div className="flex justify-center gap-3">
          <Link to="/explore" className="btn-accent inline-flex items-center gap-2 text-sm">
            Start Your Journey <ArrowRight className="w-4 h-4" />
          </Link>
          <Link to="/explore" className="btn-outline-light text-sm">Learn More</Link>
        </div>
      </div>
    </section>

    <Footer />
  </div>
);

export default About;
