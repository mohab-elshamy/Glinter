import { Compass, Sparkles } from "lucide-react";
import { Link } from "react-router-dom";
import Footer from "@/components/Footer";
import Navbar from "@/components/Navbar";

type ItineraryFeatureState =
  | { status: "unavailable"; message: string };

const itineraryState: ItineraryFeatureState = {
  status: "unavailable",
  message: "Itinerary generation is not available because the backend module has not been implemented yet.",
};

const WhereToGo = () => (
  <div className="min-h-screen bg-background">
    <Navbar />
    <main className="container mx-auto flex min-h-[70vh] max-w-3xl items-center px-4 py-12">
      <section className="card-glass w-full p-8 text-center sm:p-12">
        <div className="mx-auto mb-5 flex h-14 w-14 items-center justify-center rounded-2xl bg-primary/15">
          <Sparkles className="h-7 w-7 text-primary" />
        </div>
        <h1 className="text-3xl font-extrabold">
          Where to <span className="text-gradient-orange">Go</span>
        </h1>
        <p className="mx-auto mt-3 max-w-xl text-sm text-muted-foreground">
          {itineraryState.message}
        </p>
        <p className="mx-auto mt-2 max-w-xl text-xs text-muted-foreground">
          No sample itinerary or random prices are being shown in its place.
        </p>
        <Link
          to="/local-buddies"
          className="btn-accent mt-7 inline-flex items-center gap-2 rounded-lg px-5 py-2.5 text-sm"
        >
          <Compass className="h-4 w-4" />
          Browse live experiences
        </Link>
      </section>
    </main>
    <Footer />
  </div>
);

export default WhereToGo;
