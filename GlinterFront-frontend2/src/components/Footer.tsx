import { Link } from "react-router-dom";
import { Compass } from "lucide-react";

const Footer = () => (
  <footer className="border-t border-brand-glassBorder py-12" style={{ backgroundColor: "#0B0C10" }}>
    <div className="container mx-auto max-w-7xl px-4">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mb-8">
        <div>
          <div className="flex items-center gap-2 mb-4">
            <div className="w-6 h-6 rounded-full bg-brand-gold flex items-center justify-center text-brand-dark font-bold text-xs">
              G
            </div>
            <span className="font-bold text-lg">Glinter</span>
          </div>
          <p className="text-gray-400 text-sm">AI-powered travel platform for discovering the best of Egypt.</p>
        </div>
        <div>
          <h5 className="font-bold mb-4">Explore</h5>
          <ul className="space-y-2 text-sm text-gray-400">
            <li><Link to="/explore" className="hover:text-brand-gold transition">Home</Link></li>
            <li><Link to="/where-to-stay" className="hover:text-brand-gold transition">Where to Stay</Link></li>
            <li><Link to="/where-to-go" className="hover:text-brand-gold transition">Where to Go</Link></li>
            <li><Link to="/local-buddies" className="hover:text-brand-gold transition">Buddies</Link></li>
          </ul>
        </div>
        <div>
          <h5 className="font-bold mb-4">Support</h5>
          <ul className="space-y-2 text-sm text-gray-400">
            <li><span className="hover:text-brand-gold transition cursor-pointer">Help Center</span></li>
            <li><span className="hover:text-brand-gold transition cursor-pointer">Contact Us</span></li>
            <li><span className="hover:text-brand-gold transition cursor-pointer">Safety Resources</span></li>
          </ul>
        </div>
        <div>
          <h5 className="font-bold mb-4">Company</h5>
          <ul className="space-y-2 text-sm text-gray-400">
            <li><Link to="/about" className="hover:text-brand-gold transition">About Us</Link></li>
            <li><span className="hover:text-brand-gold transition cursor-pointer">Privacy Policy</span></li>
            <li><span className="hover:text-brand-gold transition cursor-pointer">Terms of Service</span></li>
          </ul>
        </div>
      </div>
      <div className="text-center text-xs text-gray-600 border-t border-brand-glassBorder pt-8">
        © 2025 Glinter. All rights reserved.
      </div>
    </div>
  </footer>
);

export default Footer;
