import { redirect } from 'next/navigation';

// Kök giriş — kontrol paneline yönlendir; korumalı route mantığı oradan devralır.
export default function HomePage() {
  redirect('/dashboard');
}
