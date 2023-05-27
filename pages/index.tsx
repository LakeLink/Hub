import { withIronSessionSsr } from 'iron-session/next';
import { GetStaticProps, GetStaticPaths, GetServerSideProps, InferGetStaticPropsType, InferGetServerSidePropsType } from 'next'
import Link from 'next/link';
import { links } from '~/indexLinks'
import { sessionOptions } from '~/lib/session';
import { ArrowRightOnRectangleIcon, ArrowLeftOnRectangleIcon, MagnifyingGlassIcon } from '@heroicons/react/24/solid'
import { User } from '~/lib/user';

export default function Index({
  verified,
  org,
}: InferGetServerSidePropsType<typeof getServerSideProps>) {
  let linkElements = links.map((v, idx) =>
    <div key={idx} className="flex flex-col items-center hover:scale-105 transition">
      {
        v.casRequired ?
          <Link
            className="mb-1 rounded-xl shadow-xl  bg-slate-900  hover:bg-slate-800 text-center h-12 w-12  sm:h-16 sm:w-16  md:h-20 md:w-20  flex place-items-center justify-center" href={{
              pathname: '/api/cas/to',
              query: { redirectUrl: v.url }
            }}>
            <img className="p-4" src={v.icon} />
          </Link>
          :
          <Link href={v.url}
            className="mb-1 rounded-xl shadow-xl  bg-slate-900  hover:bg-slate-800 text-center h-12 w-12  sm:h-16 sm:w-16  md:h-20 md:w-20  flex place-items-center justify-center">
            <img className="p-4" src={v.icon} />
          </Link>
      }
      {v.name}
    </div>
  )
  return <>
    <div className="max-w-screen-lg w-full mx-auto flex flex-col justify-center text-gray-50 py-6 sm:py-10">
      {
        verified ?
          <h1 className="self-center text-xl pb-10">Hi there,<br /> My friend from {org} ✨</h1>
          :
          <h1 className="self-center text-xl pb-10">Nice to meet you!</h1>
      }
      <form className="w-4/5 mx-auto mb-10 flex justify-center" method="get" action="https://fsoufsou.com/search">
        <div className="relative w-1/4 hover:w-full transition-all">
          <span className="absolute top-2 left-3 z-20">
            <MagnifyingGlassIcon className='w-6 h-6' />
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
          verified ?
            <div className="flex flex-col items-center hover:scale-105 transition">
              <Link href='/api/auth/signOut'
                className="mb-1 rounded-xl shadow-xl  bg-slate-900  hover:bg-slate-800 text-center h-12 w-12  sm:h-16 sm:w-16  md:h-20 md:w-20  flex place-items-center justify-center">
                <ArrowRightOnRectangleIcon className='w-8 h-8'></ArrowRightOnRectangleIcon>
              </Link>
              Sign Out
            </div>
            :
            <div className="flex flex-col items-center hover:scale-105 transition">
              <Link href='/auth/signIn'
                className="m-auto rounded-xl shadow-xl  bg-slate-900  hover:bg-slate-800 text-center h-12 w-12  sm:h-16 sm:w-16  md:h-20 md:w-20  flex place-items-center justify-center">
                <ArrowLeftOnRectangleIcon className='w-8 h-8'></ArrowLeftOnRectangleIcon>
              </Link>
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
  return {
    props: {
      verified: req.session.verified ?? null,
      org: req.session.org ?? null,
    }
  }
},
  sessionOptions);
