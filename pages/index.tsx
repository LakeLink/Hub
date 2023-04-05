import { withIronSessionSsr } from 'iron-session/next';
import { GetStaticProps, GetStaticPaths, GetServerSideProps, InferGetStaticPropsType, InferGetServerSidePropsType } from 'next'
import { links } from '~/indexLinks'
import { sessionOptions } from '~/lib/session';
export default function Index({
  user,
}: InferGetServerSidePropsType<typeof getServerSideProps>) {
  let linkElements = links.map((v, idx) =>
    <div key={idx} className="flex flex-col items-center hover:scale-105 transition">
      {
        v.casRequired ? <a
          className="mb-1 rounded-xl shadow-xl  bg-slate-900  hover:bg-slate-800 text-center h-12 w-12  sm:h-16 sm:w-16  md:h-20 md:w-20  flex place-items-center justify-center">
          <img className="p-4" src={v.icon} />
        </a> : <a href={v.url}
          className="mb-1 rounded-xl shadow-xl  bg-slate-900  hover:bg-slate-800 text-center h-12 w-12  sm:h-16 sm:w-16  md:h-20 md:w-20  flex place-items-center justify-center">
          <img className="p-4" src={v.icon} />
        </a>
      }
      {v.name}
    </div>
  )
  return <>
    <div className="max-w-screen-lg w-full mx-auto flex flex-col justify-center text-gray-50 py-6 sm:py-10">
      {
        user ?
        <h1 className="self-center text-xl pb-10">Hi there,<br /> My friend from {user.organization} ✨</h1>
        :
        <h1 className="self-center text-xl pb-10">Nice to meet you!</h1>
      }
      <form className="w-4/5 mx-auto mb-10 flex justify-center" method="get" action="https://fsoufsou.com/search">
        <div className="relative w-1/4 hover:w-full transition-all">
          <span className="absolute top-2 left-3 z-20">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5"
              stroke="currentColor" className="w-6 h-6">
              <path stroke-linecap="round" stroke-linejoin="round"
                d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
            </svg>
          </span>
          <input id="q" name="q"
            className="w-full px-10 py-2 z-10 backdrop-blur-sm bg-opacity-50 bg-gray-500 rounded-full"
            placeholder="Type to search..." autoComplete="off" />
        </div>
      </form>

      <div className="grid grid-cols-5 gap-y-6 justify-between justify-items-center text-sm">
        {linkElements}
      </div>
      <div className="w-full border-y border-gray-200 my-8"></div>
      <div className="grid grid-cols-5 gap-y-6 justify-between justify-items-center text-sm">
        {
          user ?
            <div className="flex flex-col items-center hover:scale-105 transition">
              <a href='/api/auth/signOut'
                className="mb-1 rounded-xl shadow-xl  bg-slate-900  hover:bg-slate-800 text-center h-12 w-12  sm:h-16 sm:w-16  md:h-20 md:w-20  flex place-items-center justify-center">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5"
                  stroke="currentColor" className="w-8 h-8">
                  <path stroke-linecap="round" stroke-linejoin="round"
                    d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9" />
                </svg>
              </a>
              Sign Out
            </div>
            :
            <div className="flex flex-col items-center hover:scale-105 transition">
              <a href='/auth/signIn'
                className="m-auto rounded-xl shadow-xl  bg-slate-900  hover:bg-slate-800 text-center h-12 w-12  sm:h-16 sm:w-16  md:h-20 md:w-20  flex place-items-center justify-center">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" className="w-8 h-8">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
                </svg>
              </a>
              Sign In
            </div>
        }
      </div></div>
  </>
}

export const getServerSideProps = withIronSessionSsr(async function ({
  req,
  res,
}) {
  const user = req.session.user;

  return {
    props: {
      user: user ? user : null
    }
  }
},
  sessionOptions);
