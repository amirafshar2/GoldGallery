"use client";

/**
 * Zoom Parallax — 21st.dev @efferd/zoom-parallax contract: `images: { src, alt }[]` (7 max), scroll-driven zoom.
 * ⚠ Component.tsx on 21st is locked without sign-in, so this is a faithful re-implementation of the same public
 * layout (the Olivier Larose zoom-parallax the 21st component is based on). Swap for the original once unlocked.
 * Tracked in docs/21st-components.md. Images only (no video) — Amir's decision.
 */
import { useRef } from "react";
import { motion, useScroll, useTransform, useReducedMotion } from "framer-motion";

export type ParallaxImage = { src: string; alt?: string };

// Position of each image relative to the centered one (same layout as the 21st demo).
const POS = [
  "w-[25vw] h-[25vh]",
  "[top:-30vh] [left:5vw] w-[35vw] h-[30vh]",
  "[top:-10vh] [left:-25vw] w-[20vw] h-[45vh]",
  "[left:27.5vw] w-[25vw] h-[25vh]",
  "[top:27.5vh] [left:5vw] w-[20vw] h-[25vh]",
  "[top:27.5vh] [left:-22.5vw] w-[30vw] h-[25vh]",
  "[top:22.5vh] [left:25vw] w-[15vw] h-[15vh]",
];
const SCALES = [4, 5, 6, 5, 6, 8, 9];

export function ZoomParallax({ images, children }: { images: ParallaxImage[]; children?: React.ReactNode }) {
  const ref = useRef<HTMLDivElement>(null);
  const reduce = useReducedMotion();
  const { scrollYProgress } = useScroll({ target: ref, offset: ["start start", "end end"] });
  const scales = SCALES.map((s) => useTransform(scrollYProgress, [0, 1], [1, s])); // eslint-disable-line react-hooks/rules-of-hooks
  const textOpacity = useTransform(scrollYProgress, [0, 0.25], [1, 0]);
  const textY = useTransform(scrollYProgress, [0, 0.25], [0, -60]);
  const veil = useTransform(scrollYProgress, [0, 0.3, 1], [0.55, 0.15, 0]);
  const pics = images.slice(0, 7);

  return (
    <section ref={ref} className="velvet relative h-[300vh]" aria-label="hero">
      <div className="sticky top-0 h-screen overflow-hidden">
        {pics.length === 0 && (
          <div className="absolute inset-0 bg-[radial-gradient(60%_60%_at_50%_40%,color-mix(in_srgb,var(--brand-primary)_40%,transparent),transparent_70%)]" />
        )}
        {pics.map((img, i) => (
          <motion.div key={img.src + i} style={reduce ? undefined : { scale: scales[i] }}
            className="absolute top-0 flex size-full items-center justify-center will-change-transform">
            <div className={`relative overflow-hidden rounded-sm shadow-2xl ${POS[i]}`}>
              <img src={img.src} alt={img.alt ?? ""} className="size-full object-cover"
                loading={i === 0 ? "eager" : "lazy"} fetchPriority={i === 0 ? "high" : "auto"} />
            </div>
          </motion.div>
        ))}
        <motion.div style={{ opacity: reduce ? 0.55 : veil }} className="pointer-events-none absolute inset-0 bg-velvet" />
        <motion.div style={reduce ? undefined : { opacity: textOpacity, y: textY }}
          className="relative z-10 grid h-full place-items-center px-4 text-center">
          {children}
        </motion.div>
      </div>
    </section>
  );
}
