import type { NextConfig } from "next";

const securityHeaders = [
  // Nobody can embed the app in a frame (clickjacking during an exam)
  { key: "X-Frame-Options", value: "DENY" },
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
  // Only this site may use the camera/mic/screen (proctoring); no geolocation
  {
    key: "Permissions-Policy",
    value: "camera=(self), microphone=(self), display-capture=(self), geolocation=()",
  },
];

const nextConfig: NextConfig = {
  // Fewer build workers: the default count runs out of memory while collecting page data on this machine
  experimental: { cpus: 2 },
  async headers() {
    return [{ source: "/:path*", headers: securityHeaders }];
  },
  // الطلبات تعدي من دومين الفرونت نفسه، فالكوكيز (te_rt) بتتبعت مع الـ logout والـ refresh
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: "https://trutheye657.runasp.net/api/:path*",
      },
    ];
  },
};

export default nextConfig;