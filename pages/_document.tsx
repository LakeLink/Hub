import { Html, Head, Main, NextScript } from 'next/document'

export default function Document() {
  return (
    <Html>
      <Head />
      <body className="bg-gray-700">
        <Main />
        <div className="mx-auto flex flex-col justify-between max-w-3xl">
          <footer className="flex justify-center border-t-2 border-slate-600">
            <div className="p-4 text-slate-500 divide-x-2 divide-slate-600">
              <a href="https://github.com/LakeLink/Hub" className="p-1"><span className="underline">LakeHub</span>, an open-source project.</a><span className="p-1">Made with 💕 by Yiffyi, LakeLink, Westlake University</span>
            </div>
          </footer>
        </div>
        <NextScript />
      </body>
    </Html>
  )
}